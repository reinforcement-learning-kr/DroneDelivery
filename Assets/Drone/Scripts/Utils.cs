using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Utils
{
    public static Color GetParcelColor(HouseEntity.PARCEL_TYPE parcelType_)
    {
        Color color = Color.white;
        switch (parcelType_)
        {
            case HouseEntity.PARCEL_TYPE.GREEN: color = Color.green; break;
            case HouseEntity.PARCEL_TYPE.BLUE: color = Color.blue; break;
            case HouseEntity.PARCEL_TYPE.RED: color = Color.red; break;
        }

        return color;
    }
}
