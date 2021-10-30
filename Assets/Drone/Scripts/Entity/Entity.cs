using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Entity : MonoBehaviour
{
    protected Transform trans;
    public Transform Trans { get { return trans; } }

    private bool isActive = false;
    public bool IsActive { get { return isActive; } }

    protected int index = -1;
    public int Index { get { return Index; } }

    public virtual void Init(Transform Trans_)
    {
        trans = Trans_;
    }

    public void SetIndex(int index_)
    {
        index = index_;
    }

    public void SetActive(bool active)
    {
        isActive = active;
        gameObject.SetActive(isActive);
    }

    public virtual void SetPosition(Vector3 pos)
    {
        trans.position = pos;
    }

    public void SetRotation(Quaternion rotation)
    {
        trans.rotation = rotation;
    }
}
