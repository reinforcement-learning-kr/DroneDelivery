using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Obstacle : Entity
{
    private Vector3 offsetPos = new Vector3(0, 3, 0);
    public override void SetPosition(Vector3 pos)
    {
        base.SetPosition(pos + offsetPos);
    }
}
