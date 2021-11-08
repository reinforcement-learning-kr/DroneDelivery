using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HouseManager : MonoSingleton<HouseManager>
{
    [SerializeField]
    private List<HouseEntity> houseList = new List<HouseEntity>();

    private HouseEntity curDestHouse = null;

    [SerializeField]
    private List<HouseEntity> curDestHouseList = new List<HouseEntity>();
    private List<HouseEntity> stateHouseList = new List<HouseEntity>();
    private List<HouseEntity> tempDestHouseList = new List<HouseEntity>();
    private List<int> destIndexList = new List<int>();

    private List<float> preDisList = new List<float>();
    private List<HouseEntity.PARCEL_TYPE> parcelList = new List<HouseEntity.PARCEL_TYPE>();
    public List<HouseEntity.PARCEL_TYPE> ParcelList { get{ return parcelList; } }
    private int maxDest = 3;
    private int maxCount = 1;
    private int remainCount = 1;
    private Difficulty gameDifficulty = Difficulty.EASY;
    private List<DroneSetting.InferenceInfo> InferenceInfoList = null;

    private Inference_Type inferenceType = Inference_Type.NONE;
    private int nearestIndex = 0;
    private int inference_index = 0;

    public int MaxDest { get { return maxDest; } }
    public int RemainCount { get { return remainCount; } }

    public List<float> goalTimeList = new List<float>();

    public delegate void EndInference();
    public EndInference endInference_del;

    public void Init(Difficulty gameDifficulty_)
    {
        for (int i = 0; i < houseList.Count; i++)
        {
            HouseEntity curHouse = houseList[i];

            curHouse.Init(curHouse.transform);
            curHouse.SetIndex(i);
            curHouse.SetGuidance(false);
        }

        gameDifficulty = gameDifficulty_;

        switch (gameDifficulty)
        {
            case Difficulty.BASIC:
            case Difficulty.EASY: maxDest = 1; break;
            case Difficulty.NORMAL: maxDest = 3; break;
            case Difficulty.HARD: maxDest = 5; break;
        }
    }

    public void Set_Inference_Info(Inference_Type type, List<DroneSetting.InferenceInfo> InferenceInfoList_)
    {
        inferenceType = type;
        InferenceInfoList = InferenceInfoList_;
    }

    public void ResetHouse()
    {
        Init(gameDifficulty);
    }

    public HouseEntity GetHouse(int index)
    {
        HouseEntity house = null;

        for (int i = 0; i < houseList.Count; i++)
        {
            if (i == index)
            {
                house = houseList[index];
                break;
            }
        }

        return house;
    }

    public void updateElapsed(float Elapsed)
    {
        for (int i = 0; i < houseList.Count; i++)
        {
            houseList[i].updateElapsed(Elapsed);
        }
    }

    public void SetDestHouseV2(Vector3 agentPos)
    {
        if (inference_index >= 10)
        {
            // if(null != endInference_del)
            //     endInference_del();
            Application.Quit();
        }

        goalTimeList.Clear();

        curDestHouseList.Clear();
        preDisList.Clear();
        parcelList.Clear();
        tempDestHouseList.Clear();
        destIndexList.Clear();
        stateHouseList.Clear();
        List<int> indexList = null;
        
        if (Inference_Type.NONE == inferenceType)
        {
            indexList = new List<int>();

            int count = 0;

            while (count < maxDest)
            {
                bool isOverlab = false;
                int index = Random.Range(0, houseList.Count);

                for (int i = 0; i < indexList.Count; i++)
                {
                    if (indexList[i] == index)
                    {
                        isOverlab = true;
                        break;
                    }
                }

                if (true == isOverlab)
                    continue;
                else
                {
                    indexList.Add(index);
                    count++;
                }
            }
        }
        else
        {
            indexList = InferenceInfoList[inference_index].houseNums;
            inference_index++;
        }

        for (int i = 0; i < houseList.Count; i++)
        {
            if (null != houseList[i])
                houseList[i].SetGuidance(false);
        }


        for (int i = 0; i < indexList.Count; i++)
        {
            int index = indexList[i];

            HouseEntity.PARCEL_TYPE curParcelType = (HouseEntity.PARCEL_TYPE)Random.Range((int)HouseEntity.PARCEL_TYPE.GREEN, (int)HouseEntity.PARCEL_TYPE.MAX);
            curParcelType = HouseEntity.PARCEL_TYPE.BLUE;
            houseList[index].SetParcel(curParcelType);
            houseList[index].SetGuidance(true);

            parcelList.Add(curParcelType);
            tempDestHouseList.Add(houseList[index]);
            preDisList.Add(Vector3.Distance(houseList[index].GetDropOffPos(), agentPos));
        }

        for(int i = 0; i < tempDestHouseList.Count; i++)
        {
            Vector3 standardPos = Vector3.zero;

            if (i == 0)
                standardPos = agentPos;
            else
                standardPos = curDestHouseList[i - 1].GetDropOffPos();

            float min_dis = int.MaxValue;
            int min_dis_index = 0;

            for (int index2 = 0; index2 < tempDestHouseList.Count; index2++)
            {
                if (curDestHouseList.Contains(tempDestHouseList[index2]))
                    continue;

                Vector3 srcPos = tempDestHouseList[index2].GetDropOffPos();
                float curDis = Vector3.Distance(srcPos, standardPos);

                if (curDis <= 0f)
                    continue;

                if (min_dis > curDis)
                {
                    min_dis_index = index2;
                    min_dis = curDis;
                }
            }

            curDestHouseList.Add(tempDestHouseList[min_dis_index]);
            stateHouseList.Add(tempDestHouseList[min_dis_index]);
            destIndexList.Add(min_dis_index);
        }

        maxCount = curDestHouseList.Count;
        remainCount = maxCount;
    }

    public Vector3 GetDestPosition()
    {
        Vector3 pos = Vector3.zero;

        int destIndex = 0;

        if (null != curDestHouseList[destIndex])
            pos = curDestHouseList[destIndex].GetDropOffPos();

        return pos;
    }

    public Vector3 GetDestPosition(int index_)
    {
        Vector3 pos = Vector3.zero;

        if(index_ < stateHouseList.Count)
        {
            if(null != stateHouseList[index_])
                pos = stateHouseList[index_].GetDropOffPos();
        }

        return pos;
    }

    public bool CheckDeliveryComplete(Vector3 agentPos, float dropOffDistance, float time)
    {
        bool IsDeliveryComplete = false;

        IsDeliveryComplete = CheckNotDestDeliveryComplete(agentPos, dropOffDistance);

        if(false == IsDeliveryComplete)
        {
            int destIndex = 0;

            float curDis = Vector3.Distance(curDestHouseList[destIndex].GetDropOffPos(), agentPos);

            if (curDis < dropOffDistance)
            {
                remainCount = DeliveryComplete(destIndex);
                IsDeliveryComplete = true;
            }
        }

        if (true == IsDeliveryComplete)
            goalTimeList.Add(time);

        return IsDeliveryComplete;
    }

    public bool CheckNotDestDeliveryComplete(Vector3 agentPos, float dropOffDistance)
    {
        int destIndex = 0;
        bool IsDeliveryComplete = false;

        for (int i = 0; i < curDestHouseList.Count; i++)
        {
            if (i == destIndex)
                continue;

            float curDis = Vector3.Distance(curDestHouseList[i].GetDropOffPos(), agentPos);

            if (curDis < dropOffDistance)
            {
                remainCount = DeliveryComplete(i);
                IsDeliveryComplete = true;
                RearrangeDestHouse(agentPos);
                break;
            }
        }

        return IsDeliveryComplete;
    }

    public void RearrangeDestHouse(Vector3 agentPos)
    {
        if(curDestHouseList.Count > 0)
            curDestHouseList.Sort((x, y) => Vector3.Distance(x.GetDropOffPos(), agentPos).CompareTo(Vector3.Distance(y.GetDropOffPos(), agentPos)));
    }

    public int DeliveryComplete(int index)
    {
        if (null != curDestHouseList[index])
        {
            curDestHouseList[index].SetGuidance(false);
            curDestHouseList.RemoveAt(index);
            preDisList.RemoveAt(index);
        }

        remainCount--;

        return remainCount;
    }

    public float GetProgress()
    {
        return (float)remainCount / maxCount;
    }

    public HouseEntity.PARCEL_TYPE GetCurdestPacelType()
    {
        HouseEntity.PARCEL_TYPE parcel_type = HouseEntity.PARCEL_TYPE.NONE;
        if (null != curDestHouse)
            parcel_type = curDestHouse.Parcel_type;

        return parcel_type;
    }
}
