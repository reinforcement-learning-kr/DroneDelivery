using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class InferenceResult
{
    public int episodeNum;
    public int deliveryCompleteCount;
    public float DurationOfTime;
    public float rawScore;
    public List<float> goalTimeList;

    public void CalculateOverall()
    {
        if (0 == deliveryCompleteCount)
            return;

        float sum = 0f;

        for (int i = 0; i < goalTimeList.Count; i++)
            sum += goalTimeList[i];

        DurationOfTime = sum / deliveryCompleteCount;
    }
}
