using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HouseEntity : Entity
{
    public enum PARCEL_TYPE : int
    {
        NONE = 0,
        GREEN,
        BLUE,
        RED,
        MAX
    }

    private Vector3 initPos = Vector3.zero;
    private PARCEL_TYPE parcel_type = PARCEL_TYPE.GREEN;
    public PARCEL_TYPE Parcel_type { get { return parcel_type;} }
    private int groupIndex = -1;
    public int GroupIndex { get { return groupIndex; } }
    [SerializeField]
    private GameObject guidamceObj = null;

    [SerializeField]
    private List<MeshRenderer> dropOffMeshRenderer = null;
    [SerializeField]
    private Transform dropOffTrans = null;
    [SerializeField]
    private GameObject bigArrow = null;

    public override void Init(Transform Trans_)
    {
        base.Init(Trans_);

        parcel_type = (PARCEL_TYPE)Random.Range((int)PARCEL_TYPE.GREEN, (int)PARCEL_TYPE.MAX);
        SetDropOffColor();
        bigArrow = guidamceObj.transform.Find("ArrowParent").gameObject;
       // bigArrow.SetActive(false);
    }

    public void SetInitPos(Vector3 pos)
    {
        initPos = pos;
    }

    public void SetDropOffColor()
    {
        if (null == dropOffMeshRenderer)
            return;

        for(int i = 0; i < dropOffMeshRenderer.Count; i++)
        {
            if (null == dropOffMeshRenderer[i])
                continue;

            dropOffMeshRenderer[i].material.color = Utils.GetParcelColor(parcel_type);
        }
    }

    public void SetParcel(PARCEL_TYPE parcel_type_)
    {
        parcel_type = parcel_type_;
        SetDropOffColor();
    }

    public void SetGroupIndex(int groupIndex_)
    {
        groupIndex = groupIndex_;
    }

    public void updateElapsed(float Elapsed)
    {
       
    }

    public void SetGuidance(bool active)
    {
        if (null != guidamceObj)
            guidamceObj.SetActive(active);
    }

    public Vector3 GetDropOffPos()
    {
        Vector3 pos = Vector3.zero;

        if (null != dropOffTrans)
            pos = dropOffTrans.position;

        return pos;
    }
}
