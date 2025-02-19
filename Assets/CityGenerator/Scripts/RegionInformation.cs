﻿using UnityEngine;

public class RegionInformation : MonoBehaviour
{
    public float DistToCenter;
    public float Slope;
    public float WaterProximity;
    public float Privacy;
    public float View;
    public float SunExposure;
    public float WeightDistToCenter;
    public float WeightSlope;
    public float WeightWaterProximity;
    public float WeightPrivacy;
    public float WeightView;
    public float WeightSunExposure;
    public float Height;
    public int Accommodates;

    internal void CopyInformationFrom(RegionInformation ri2)
    {
        DistToCenter = ri2.DistToCenter;
        Slope = ri2.Slope;
        WaterProximity = ri2.WaterProximity;
        Privacy = ri2.Privacy;
        View = ri2.View;
        SunExposure = ri2.SunExposure;
        WeightDistToCenter = ri2.WeightDistToCenter;
        WeightSlope = ri2.WeightSlope;
        WeightWaterProximity = ri2.WeightWaterProximity;
        WeightPrivacy = ri2.WeightPrivacy;
        WeightView = ri2.WeightView;
        WeightSunExposure = ri2.WeightSunExposure;
        Height = ri2.Height;
        Accommodates = ri2.Accommodates;
    }
}
