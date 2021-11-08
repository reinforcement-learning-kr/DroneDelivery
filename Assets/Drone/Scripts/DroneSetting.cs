using UnityEngine;
using Unity.MLAgents;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;

public enum Inference_Type : int
{
    NONE,
    ML_AGENT,
    PYTHON
}

public enum Difficulty : int 
{   
    BASIC,
    EASY,
    NORMAL,
    HARD
}

public class DroneSetting : MonoBehaviour
{
    public GameObject DroneAgent;
    public int House_count = 0;

    [System.Serializable]
    public class UserSetParams
    {
        [SerializeField]
        public float reward = 0;
        [SerializeField]
        public float penalty = 0;
        [SerializeField]
        public float distanceRewardScale = 0;
    }


    [System.Serializable]
    public class Parameters
    {
        [SerializeField]
        public int basicMaxStep = 0;
        [SerializeField]
        public int easyMaxStep = 0;
        [SerializeField]
        public int normalMaxStep = 0;
        [SerializeField]
        public int hardMaxStep = 0;
        [SerializeField]
        public int horiRayCount = 0;
        [SerializeField]
        public float horiRayDis = 0;
        [SerializeField]
        public float bottomRayDis = 0;
        [SerializeField]
        public float dropOffDistance = 0;
        [SerializeField]
        public float wareHouseDistance = 0;
        [SerializeField]
        public float movableRange = 0;
        [SerializeField]
        public int defaultObsSize = 0;
        [SerializeField]
        public float topRayDis = 0;
        [SerializeField]
        public int verticalRayCount = 0;
        [SerializeField]
        public float verticalRayDis = 0;
    }

    [System.Serializable]
    public class InferenceInfoData
    {
        [SerializeField]
        public List<InferenceInfo> InferenceInfos = null;
    }


    [System.Serializable]
    public class InferenceInfo
    {
        [SerializeField]
        public int id = 0;
        [SerializeField]
        public List<int> houseNums = null;
        [SerializeField]
        public int randomSeed = 0;
    }

    private HouseManager houseManager = null;

    [Header("[모드 선택]")]
    [SerializeField]
    private Inference_Type inferenceType = Inference_Type.NONE;
    public bool giveAllHouse = false;
    [SerializeField]
    public List<InferenceInfo> InferenceInfoList = null;

    [Header("[난이도 선택]")]
    [SerializeField]
    private Difficulty gameDifficulty;

    [SerializeField]
    private DroneAgent agent = null;
    [SerializeField]
    private GameObject Trigger = null;
    [SerializeField]
    private Transform UI_trans = null;
    private UI_Main mainUI = null;

    private Vector3 wareHousePos = Vector3.zero;
    public Vector3 WareHousePos { get { return wareHousePos; } }
    private Vector3 areaPos = Vector3.zero;
    public Vector3 AreaPos { get { return areaPos; } }

    private Vector3 droneInitPos = Vector3.zero;

    [SerializeField]
    private Transform cameraTrans = null;
    [SerializeField]
    private Transform DroneTrans = null;

    private Vector3 cameraOffset = Vector3.zero;

    [Header("[여러가지 parameters 설정]")]
    [SerializeField]
    public Parameters parameters;
    [SerializeField]
    public UserSetParams userSetParams;
    [SerializeField]
    public TotalInferenceResult totalInferenceResult = new TotalInferenceResult();

    public Inference_Type InferenceType
    {
        get { return inferenceType; }
    }


    public bool IsDelivery 
    { 
        get 
        {
            bool state = false;

            if(null != Trigger)
                state = !Trigger.activeInHierarchy;

            return state; 
        } 
    }


    void Start()
    {
        Load_Inference_Data();
        Load_Params();

        if (null != agent)
        {
            agent.SetDelagte(EnterWareHouseTrigger, EndEpisode);

            switch (gameDifficulty)
            {
                case Difficulty.BASIC: agent.SetMaxStep(parameters.basicMaxStep); break;
                case Difficulty.EASY: agent.SetMaxStep(parameters.easyMaxStep); break;
                case Difficulty.NORMAL: agent.SetMaxStep(parameters.normalMaxStep); break;
                case Difficulty.HARD: agent.SetMaxStep(parameters.hardMaxStep); break;
            }
        }

        if (null != Trigger)
            wareHousePos = Trigger.transform.position;

        areaPos = transform.position;
        LoadPrefabs();

        houseManager = HouseManager.Instance;
        houseManager.Init(gameDifficulty);
        houseManager.Set_Inference_Info(inferenceType, InferenceInfoList);

        if (null != DroneAgent)
        {
            droneInitPos = DroneAgent.transform.position;
            DroneAgent.SetActive(true);
        }

        if (null != DroneTrans && null != cameraTrans)
            cameraOffset = cameraTrans.position - DroneTrans.position;

        houseManager.endInference_del = EndInference;
        totalInferenceResult.inferenceResults = new List<InferenceResult>();
    }

    public void Load_Params()
    {
        string path = string.Format("{0}/StreamingAssets", Application.dataPath);

        string json_Str = string.Empty;
        json_Str = File.ReadAllText(string.Format("{0}/Parameters.json", path));

        userSetParams = JsonConvert.DeserializeObject<UserSetParams>(json_Str);
    }

    public void Load_Inference_Data()
    {
        if (Inference_Type.NONE == inferenceType)
            return;

        string path = string.Empty;
        string json_Str = string.Empty;

        if (Application.platform == RuntimePlatform.OSXPlayer)
        {
            path = string.Format("{0}/Resources/Data/StreamingAssets/Inference_Data", Application.dataPath);
            json_Str = string.Empty;
        }
        else
        {
            path = string.Format("{0}/StreamingAssets/Inference_Data", Application.dataPath);
            json_Str = string.Empty;
        }

        path = string.Format("{0}/Inference_Data", Application.streamingAssetsPath);
        json_Str = string.Empty;

        switch (gameDifficulty)
        {
            case Difficulty.BASIC:
            case Difficulty.EASY:
                json_Str = File.ReadAllText(string.Format("{0}/Easy.json", path));
                break;
            case Difficulty.NORMAL:
                json_Str = File.ReadAllText(string.Format("{0}/Normal.json", path));
                break;
        }

        InferenceInfoData inferData = JsonConvert.DeserializeObject<InferenceInfoData>(json_Str);

        InferenceInfoList = inferData.InferenceInfos;

        Debug.Log(InferenceInfoList);
    }

    public void LoadPrefabs()
    {
        GameObject townPrefab = null;

        switch(gameDifficulty)
        {
            case Difficulty.BASIC:
                townPrefab = Resources.Load("Prefabs/Town1") as GameObject;
                break;
            case Difficulty.EASY:
                townPrefab = Resources.Load("Prefabs/Town2") as GameObject;
                break;
            case Difficulty.NORMAL:
                townPrefab = Resources.Load("Prefabs/Town2") as GameObject;
                break;
            case Difficulty.HARD:
                townPrefab = Resources.Load("Prefabs/Town2") as GameObject;
                break;
        }

        GameObject townIns = GameObject.Instantiate(townPrefab);
        townIns.transform.SetParent(transform);


        GameObject mainUIPrefab = Resources.Load("Prefabs/UI_Main") as GameObject;
        GameObject mainUIIns = GameObject.Instantiate(mainUIPrefab);
        mainUIIns.transform.SetParent(UI_trans);
        mainUIIns.transform.localPosition = Vector3.zero;
        mainUIIns.transform.localScale = Vector3.one;

        mainUI = mainUIIns.GetComponent<UI_Main>();
    }

    int seedIndex = 0;
    public void AreaSetting()
    {
        if (null != Trigger)
            Trigger.SetActive(false);

        SetInferenceResult();

        houseManager.ResetHouse();

        EnterWareHouseTrigger();

        if (null != agent)
        {
            agent.SetAcitveParcel(false);
            agent.SetPosttion(droneInitPos);
            agent.LoadFirstHouseInfos();
        }

        if (null != mainUI)
            mainUI.CloseDonePopup();

        if (Inference_Type.NONE != inferenceType)
        {
            Random.InitState(InferenceInfoList[seedIndex].randomSeed);
            seedIndex++;
        }

        if (null != agent.birdCon)
            agent.birdCon.Re_position();

        episode++;
    }

    int episode = 0;
    public void SetInferenceResult()
    {
        if (Inference_Type.NONE == inferenceType)
            return;

        if (0 == episode)
            return;

        InferenceResult curResult = new InferenceResult();
        curResult.episodeNum = episode;
        curResult.deliveryCompleteCount = houseManager.MaxDest - houseManager.RemainCount;
        curResult.goalTimeList = new List<float>();
        curResult.goalTimeList.AddRange(houseManager.goalTimeList);
        curResult.rawScore = agent.curEpisodeReward;

        curResult.CalculateOverall();

        totalInferenceResult.inferenceResults.Add(curResult);

        string path = string.Format("{0}/StreamingAssets", Application.dataPath);

        string json_data = JsonConvert.SerializeObject(totalInferenceResult, Formatting.Indented);
        File.WriteAllText(path + "/InferenceResult.json", json_data);
    }

    public void EndInference()
    {
        string path = string.Format("{0}/StreamingAssets", Application.dataPath);

        string json_data = JsonConvert.SerializeObject(totalInferenceResult, Formatting.Indented);
        File.WriteAllText(path + "/InferenceResult.json", json_data);

        //Application.Quit();
    }

    public void EnterWareHouseTrigger()
    {
        if (null != Trigger)
            Trigger.SetActive(false);

        houseManager.SetDestHouseV2(agent.DroneTrans.position);
        //houseManager.SetCurrentDestHouse(agent.DroneTrans.position);

        if(null != agent)
            agent.SetAcitveParcel(true, houseManager.GetCurdestPacelType());
    }

    public void Update()
    {
        float elapesd = Time.deltaTime;

        if(null != houseManager)
            houseManager.updateElapsed(elapesd);

        SetUI(elapesd);

        if (null != DroneTrans && null != cameraTrans)
            cameraTrans.position = DroneTrans.position + cameraOffset;


#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Space))
            AreaSetting();
#endif
    }

    public void EndEpisode(DroneAgent.DoneType doneType)
    {
        if (null != mainUI)
            mainUI.OpenDonePopup(doneType);
    }

    public void SetUI(float elapesd)
    {
        if (null != agent)
        {
            agent.updateElapsed(elapesd);
            if (null != mainUI)
            {
                mainUI.SetStepLabel(agent.GetStepCount());
                mainUI.SetVelocityLabel(agent.GetVelocity());
                mainUI.SetRewardLabel(agent.GetCumulativeReward());
                mainUI.SetProgressLabel(houseManager.MaxDest - houseManager.RemainCount, houseManager.MaxDest, IsDelivery);
            }
        }
    }
}
