using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class FragmentsProperties : MonoBehaviour
{
    public float minThickness = Constants.DefaultMinThickness;
    public float maxThickness = Constants.DefaultMaxThickness;
    public int sitesPerTriangle = Constants.DefaultSites;
    public float maxArea = Constants.DefaultMaxArea;
    public float density = Constants.DefaultDensity;
    public Dictionary<string, float> desnities = new Dictionary<string, float>();

    public static void SetDefaults(FragmentsProperties fp)
    {
        fp.minThickness = Constants.DefaultMinThickness;
        fp.maxThickness = Constants.DefaultMaxThickness;
        fp.sitesPerTriangle = Constants.DefaultSites;
        fp.maxArea = Constants.DefaultMaxArea;
        fp.density = Constants.DefaultDensity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Copy(FragmentsProperties dst, FragmentsProperties src)
    {
        dst.minThickness = src.minThickness;
        dst.maxThickness = src.maxThickness;
        dst.sitesPerTriangle = src.sitesPerTriangle;
        dst.maxArea = src.maxArea;
        dst.density = src.density;
        dst.desnities = new Dictionary<string, float>(src.desnities);
    }

    override public string ToString()
    {
        return $"Min Thickness: {minThickness}," +
               $" Max Thickness: {maxThickness}," +
               $" Sites Per Triangle: {sitesPerTriangle}," +
               $" Max Fragment Area: {maxArea}";
    }
}
