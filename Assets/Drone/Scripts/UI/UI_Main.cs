using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_Main : MonoBehaviour
{
    [SerializeField]
    private UILabel rewardLabel = null;
    [SerializeField]
    private UILabel progressLabel = null;
    [SerializeField]
    private UILabel velocityLabel = null;
    [SerializeField]
    private UILabel stepLabel = null;
    [SerializeField]
    private UILabel donePopupMsgLabel = null;
    [SerializeField]
    private GameObject donePopup = null;

    public void SetRewardLabel(float reward)
    {
        if (null != rewardLabel)
            rewardLabel.text = string.Format("{0:f2}", reward);
    }

    public void SetStepLabel(int step)
    {
        if (null != stepLabel)
            stepLabel.text = string.Format("{0}", step);
    }

    public void SetProgressLabel(int cur, int max, bool isDelivery = false)
    {
        if (null != progressLabel)
        {
            if(true == isDelivery)
                progressLabel.text = string.Format("{0}/{1}", cur, max);
            else
                progressLabel.text = string.Format("{0}", "»óÂ÷ Áß");
        }
    }

    public void SetVelocityLabel(Vector3 velocity)
    {
        if (null != velocityLabel)
            velocityLabel.text = string.Format("X: {0:f1}  Y: {1:f1}  Z: {2:f1}", velocity.x, velocity.y, velocity.z);
    }

    public void OpenDonePopup(DroneAgent.DoneType doneType)
    {
        if (null != donePopup)
            donePopup.SetActive(true);

        if (null != donePopupMsgLabel)
            donePopupMsgLabel.text = doneType.ToString();
    }

    public void CloseDonePopup()
    {
        if (null != donePopup)
            donePopup.SetActive(false);
    }
}
