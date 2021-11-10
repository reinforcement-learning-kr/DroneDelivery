using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using PA_DronePack;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents.Policies;
using Unity.Barracuda;

public class DroneAgent : Agent
{
    public enum DoneType : int
    {
        complete,
        crush,
        spaceout,
        maxstep
    }

    private PA_DroneController dcoScript;
    public DroneSetting area;

    HouseManager houseManager = null;

    [SerializeField]
    private GameObject Parcel = null;
    [SerializeField]
    private MeshRenderer parcelMesh = null;
    [SerializeField]
    private Transform parcelTrans = null;
    [SerializeField]
    private DecisionRequester decisionRequester = null;
    [SerializeField]
    private List<Transform> droneCameraTrans = null;

    [SerializeField]
    public birdController birdCon = null;

    [SerializeField]
    BehaviorParameters behaviorParam = null;

    BrainParameters brainParam = null;

    private Transform Trans = null;
    public Transform DroneTrans { get { return Trans; } }

    private Vector3 offset = new Vector3(0, -0.3f, 0);
    [SerializeField]
    private List<Vector3> cameraOffsetList = null;
    private Vector3 cameraOffset = new Vector3(0f, 0f, 0.45f);

    private Rigidbody droneRigidbody = null;
    float preDis = 0f;

    public delegate void EnterWareHouseTrigger();
    public EnterWareHouseTrigger EnterWareHouseTrigger_del = null;

    public delegate void EpisodeDoneEvent(DoneType doneType);
    public EpisodeDoneEvent episodeDone_del = null;

    private int maxStepByDifficulty;

    [Header("[레이 캐스트 관련]")]
    [SerializeField]
    private int horiRayCount = 5;
    [SerializeField]
    private float rayDis = 1f;
    [SerializeField]
    private RaycastHit hori_rayHit;
    private float horiRayAngleInterval = 0;
    private Ray hori_ray;
    [SerializeField]
    private float bottomRayDis = 0.3f;
    private RaycastHit bottom_rayHit;
    private Ray bottom_ray;
    private float topRayDis = 0.3f;
    private RaycastHit top_rayHit;
    private Ray top_ray;
    [SerializeField]
    private int verticalRayCount = 5;
    private RaycastHit vertical_rayHit;
    private float verticalRayAngleInterval = 0;
    private Ray vertical_ray;
    [SerializeField]
    private float verticalRayDis = 1f;


    private float dropOffDistance = 1.0f;
    private float wareHouseDistance = 1.5f;

    private int decisionRequestTime = 1;

    private float episodeTime = 0f;
    public float EpisodeTime { get { return episodeTime; } }

    public float curEpisodeReward = 0f;

    public override void Initialize()
    {
        if (null != birdCon)
            birdCon.Init();

        dropOffDistance = area.parameters.dropOffDistance;
        wareHouseDistance = area.parameters.wareHouseDistance;

        houseManager = HouseManager.Instance;
        dcoScript = gameObject.GetComponent<PA_DroneController>();
        Trans = transform;
        droneRigidbody = gameObject.GetComponent<Rigidbody>();

        dcoScript.fallAfterCollision = false;
        dcoScript.sparkSound = null;

        if (null != decisionRequester)
            decisionRequestTime = decisionRequester.DecisionPeriod;

        brainParam = behaviorParam.BrainParameters;

        horiRayCount = area.parameters.horiRayCount;
        rayDis = area.parameters.horiRayDis;
        bottomRayDis = area.parameters.bottomRayDis;
        verticalRayCount = area.parameters.verticalRayCount;
        verticalRayDis = area.parameters.verticalRayDis;

        SetInputSize(area.parameters);

        if(Inference_Type.ML_AGENT == area.InferenceType)
        {
            NNModel onnxModel = Resources.Load<NNModel>("MyBehavior");
            behaviorParam.Model = onnxModel;
        }

        hori_ray = new Ray();
        hori_ray.origin = Trans.position;
        hori_ray.direction = Trans.forward;

        bottom_ray = new Ray();
        bottom_ray.origin = Trans.position;
        bottom_ray.direction = -Trans.up;

        top_ray = new Ray();
        top_ray.origin = Trans.position;
        top_ray.direction = Trans.up;

        vertical_ray = new Ray();
        vertical_ray.origin = Trans.position;
        vertical_ray.direction = Trans.up;

        horiRayAngleInterval = 360 / horiRayCount;
        verticalRayAngleInterval = 360 / verticalRayCount;

        //this.MaxStep = maxStepByDifficulty * decisionRequestTime;
        this.MaxStep = 0;
    }

    public void SetInputSize(DroneSetting.Parameters parameters)
    {
        int defaultObsSize = parameters.defaultObsSize;

        if (true == area.giveAllHouse)
            brainParam.VectorObservationSize = defaultObsSize + 3 * houseManager.MaxDest + horiRayCount * 2 + verticalRayCount * 2 + 4;
        else
            brainParam.VectorObservationSize = defaultObsSize + 3 + horiRayCount * 2 + verticalRayCount * 2 + 4;
    }

    public void SetMaxStep(int maxStep_)
    {
        maxStepByDifficulty = maxStep_;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (true == area.giveAllHouse)
        {
            for (int i = 0; i < houseManager.MaxDest; i++)
                sensor.AddObservation(houseManager.GetDestPosition(i));
        }
        else
            sensor.AddObservation(houseManager.GetDestPosition(houseManager.MaxDest - houseManager.RemainCount));

        // agent 위치 size == 3
        sensor.AddObservation(Trans.position);

        // agent velocity size == 6
        sensor.AddObservation(droneRigidbody.velocity);
        sensor.AddObservation(droneRigidbody.angularVelocity);

        // progress
        sensor.AddObservation(houseManager.GetProgress());

        sensor.AddObservation(CheckHorizontalRay());
        sensor.AddObservation(CheckVerticalRay());
        sensor.AddObservation(CheckTopandBottomRay());
       
    }

    public void SetDelagte(EnterWareHouseTrigger EnterWareHouseTrigger_del_, EpisodeDoneEvent episodeDone_del_)
    {
        EnterWareHouseTrigger_del = EnterWareHouseTrigger_del_;
        episodeDone_del = episodeDone_del_;
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        SetReward(-0.01f);

        var disAction = actionBuffers.DiscreteActions;
        var conActions = actionBuffers.ContinuousActions;

        float moveX = Mathf.Clamp(conActions[0], -1, 1f);
        float moveZ = Mathf.Clamp(conActions[1], -1, 1f);
        float moveY = Mathf.Clamp(conActions[2], -1, 1f);

        dcoScript.DriveInput(moveX);
        dcoScript.StrafeInput(moveZ);
        dcoScript.LiftInput(moveY);


        Trans.rotation = Quaternion.Euler(Trans.rotation.eulerAngles.x, 0, Trans.rotation.eulerAngles.z);

        Vector3 targetPos = Vector3.zero;
        float destDis = 0f;

        float dis = 0f;

        if (true == area.IsDelivery)
        {
            Vector3 destPos = houseManager.GetDestPosition();
            if (true == houseManager.CheckDeliveryComplete(Trans.position, dropOffDistance, episodeTime))
            {
                episodeTime = 0f;
                if (0 == houseManager.RemainCount)
                    ProcEpisodeEnd(DoneType.complete, area.userSetParams.reward);
                else
                {
                    SetReward(area.userSetParams.reward);
                    SetAcitveParcel(true, houseManager.GetCurdestPacelType());
                }
            }
            else
            {
                float cur_dist = Vector3.Distance(destPos, Trans.position);
                float reward = preDis - cur_dist;
                SetReward(reward * area.userSetParams.distanceRewardScale);
                preDis = cur_dist;
            }
        }
        else
        {
            targetPos = area.WareHousePos;
            destDis = wareHouseDistance;

            dis = Vector3.Magnitude(Trans.position - targetPos);

            if (dis < destDis)
            {
                if (null != EnterWareHouseTrigger_del)
                    EnterWareHouseTrigger_del();

                SetReward(area.userSetParams.reward);

                Vector3 destPos = houseManager.GetDestPosition();
                preDis = Vector3.Distance(destPos, Trans.position);
            }
            else
            {
                SetReward((preDis - dis) * area.userSetParams.distanceRewardScale);
                preDis = dis;
            }
        }

        float disFromArea = Vector3.Magnitude(Trans.position - area.AreaPos);
        if (disFromArea > area.parameters.movableRange)
        {
            ProcEpisodeEnd(DoneType.spaceout, area.userSetParams.penalty);
        }

        bottom_ray.origin = Trans.position;
        bottom_ray.direction = Vector3.down;

        if (Physics.Raycast(bottom_ray.origin, bottom_ray.direction, out bottom_rayHit, 0.3f))
            ProcEpisodeEnd(DoneType.crush, area.userSetParams.penalty);

        curEpisodeReward = GetCumulativeReward();

        if(StepCount >= maxStepByDifficulty * decisionRequestTime)
        {
            ProcEpisodeEnd(DoneType.maxstep, 0);
        }
    }

    public void LoadFirstHouseInfos()
    {
        Vector3 destPos = houseManager.GetDestPosition();
        preDis = Vector3.Distance(destPos, Trans.position);
    }

    public List<float> CheckVerticalRay()
    {
        List<float> raySensorList = new List<float>();

        for (int i = 0; i < verticalRayCount; i++)
        {
            Quaternion rotaion = Quaternion.Euler(0, 0, i * verticalRayAngleInterval);
            vertical_ray.origin = Trans.position;
            vertical_ray.direction = rotaion * Vector3.up;

            if (Physics.Raycast(vertical_ray.origin, vertical_ray.direction, out vertical_rayHit, verticalRayDis))
            {
                raySensorList.Add(0);
                raySensorList.Add(vertical_rayHit.distance);
            }
            else
            {
                raySensorList.Add(1);
                raySensorList.Add(1);
            }
        }

        return raySensorList;
    }

    public List<float> CheckHorizontalRay()
    {
        List<float> raySensorList = new List<float>();

        for (int i = 0; i < horiRayCount; i++)
        {
            Quaternion rotaion = Quaternion.Euler(0, i * horiRayAngleInterval, 0);
            hori_ray.origin = Trans.position;
            hori_ray.direction = rotaion * Vector3.forward;

            if (Physics.Raycast(hori_ray.origin, hori_ray.direction, out hori_rayHit, rayDis))
            {
                raySensorList.Add(0);
                raySensorList.Add(hori_rayHit.distance);
            }
            else
            {
                raySensorList.Add(1);
                raySensorList.Add(1);
            }
        }

        return raySensorList;
    }

    public List<float> CheckTopandBottomRay()
    {
        List<float> sensor = new List<float>();

        bottom_ray.origin = Trans.position;
        bottom_ray.direction = Vector3.down;

        if (Physics.Raycast(bottom_ray.origin, bottom_ray.direction, out bottom_rayHit, bottomRayDis))
        {
            sensor.Add(0);
            sensor.Add(bottom_rayHit.distance);
        }
        else
        {
            sensor.Add(1);
            sensor.Add(1);
        }

        top_ray.origin = Trans.position;
        top_ray.direction = Vector3.up;

        if (Physics.Raycast(top_ray.origin, top_ray.direction, out top_rayHit, topRayDis))
        {
            sensor.Add(0);
            sensor.Add(top_rayHit.distance);
        }
        else
        {
            sensor.Add(1);
            sensor.Add(1);
        }

        return sensor;
    }

    public void DrawRay()
    {
        for (int i = 0; i < horiRayCount; i++)
        {
            Quaternion rotaion = Quaternion.Euler(0, i * horiRayAngleInterval, 0);
            Vector3 start = hori_ray.direction;
            Vector3 curDir = rotaion * start;
            Debug.DrawRay(hori_ray.origin, curDir * rayDis, Color.green);
        }

        for (int i = 0; i < verticalRayCount; i++)
        {
            Quaternion rotaion = Quaternion.Euler(0, 0 , i * verticalRayAngleInterval);
            Vector3 start = vertical_ray.direction;
            Vector3 curDir = rotaion * start;
            Debug.DrawRay(vertical_ray.origin, curDir * verticalRayDis, Color.blue);
        }

        Debug.DrawRay(bottom_ray.origin, bottom_ray.direction * bottomRayDis, Color.yellow);
        Debug.DrawRay(top_ray.origin, top_ray.direction * topRayDis, Color.yellow);
    }

#if UNITY_EDITOR
    public void OnDrawGizmos()
    {
        DrawRay();
    }
#endif

    private void ProcEpisodeEnd(DoneType doneType, float reward)
    {
        if (null != episodeDone_del)
            episodeDone_del(doneType);

        if (null != droneRigidbody)
        {
            droneRigidbody.angularVelocity = Vector3.zero;
            droneRigidbody.velocity = Vector3.zero;
            droneRigidbody.ResetCenterOfMass();

            dcoScript.rigidBody.angularVelocity = Vector3.zero;
            dcoScript.rigidBody.velocity = Vector3.zero;
        }

        SetReward(reward);
        EndEpisode();

        area.seedIndex++;
        houseManager.inference_index++;
    }
    int count = 1;
    public override void OnEpisodeBegin()
    {
        area.AreaSetting();
        episodeTime = 0f;
        SetAcitveParcel(false);

        Rigidbody temp = GetComponent<Rigidbody>();

        if(null != temp)
        {
           // droneRigidbody.Speed
            droneRigidbody.ResetInertiaTensor();
            droneRigidbody.velocity = Vector3.zero;
            droneRigidbody.angularVelocity = Vector3.zero;
            dcoScript.DriveInput(0);
            dcoScript.StrafeInput(0);
            dcoScript.LiftInput(0);

            Trans.rotation = Quaternion.identity;
        }

        // if (null != birdCon)
        // birdCon.Re_position();
      //  preDis = Vector3.Magnitude(Trans.position - area.WareHousePos);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;

        continuousActionsOut[0] = Input.GetAxis("Vertical");
        continuousActionsOut[1] = Input.GetAxis("Horizontal");

        continuousActionsOut[2] = 0f;

        if (Input.GetKey(KeyCode.Z))
            continuousActionsOut[2] = 1f;
        if (Input.GetKey(KeyCode.X))
            continuousActionsOut[2] = -1f;
    }

    private void OnCollisionEnter(Collision collision)
    {
        switch (collision.gameObject.tag)
        {
            case "Obstacle":
            case "building":
            case "Ground":
            case "Bird": // 이정우: bird 추가
                ProcEpisodeEnd(DoneType.crush, area.userSetParams.penalty);
                break;
        }
    }

    public void SetAcitveParcel(bool active_, HouseEntity.PARCEL_TYPE parcel_type_ = HouseEntity.PARCEL_TYPE.NONE)
    {
        if (null != Parcel)
            Parcel.SetActive(true);

        if (null != parcelMesh)
            parcelMesh.material.color = Utils.GetParcelColor(parcel_type_);
    }

    public void updateElapsed(float Elapsed)
    {
        if(null != Trans && null != parcelTrans)
        {
            // parcelTrans.position = Trans.position + offset;

            parcelTrans.SetParent(Trans);
            parcelTrans.localPosition = new Vector3(0, -0.6f, 0);

            for (int i = 0; i < droneCameraTrans.Count; i++)
                droneCameraTrans[i].position = Trans.position + cameraOffsetList[i];
        }

        if (null != birdCon)
            birdCon.updateElapsed(Elapsed);

        episodeTime += Elapsed;
    }

    public Vector3 GetVelocity()
    {
        Vector3 velocity = Vector3.zero;

        if (null != droneRigidbody)
            velocity = droneRigidbody.velocity;

        return velocity;
    }

    public void SetPosttion(Vector3 position)
    {
        if (null != Trans)
            Trans.position = position;
    }

    public int GetStepCount()
    {
        int count = 0;

        count = StepCount / decisionRequestTime;

        return count;
    }
}
