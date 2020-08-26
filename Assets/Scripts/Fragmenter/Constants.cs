using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Constants
{
    public const string DestroyableStr = "Destroyable";
    public const string FragmentStr = "Fragmented";
    public const string FrozenFragmentStr = "FrozenFragment";
    public const string MovingFragmentStr = "MovingFragment";

    // Fragmenter properties
    public const float DefaultMinThickness = 0.3f;
    public const float DefaultMaxThickness = 0.35f;
    public const int   DefaultSites = 20;
    public const float DefaultMaxArea = 50.0f;
    public const long  MaxTimeMs = 15L;
    public const float DefaultDensity = 2300.0f;

    public const float MinContraction = 0.2f;
    public const float MaxContraction = 0.3f;

    public const float VoronoiScale = 500.0f;
    public const float MeshUpscaling = 1.1f;
    public const float MeshDownscaling = 1.0f;

    public const float BuildExplosionRadius = 1.0f;
    public const float BuildExplosionForce = 300.0f;

    public const float MiscExplosionRadius = 0.5f;
    public const float MiscExplosionForce = 3.0f;

    public const float MinSurfaceValue = 0.1f;
    public const float MaxSurfaceValue = 1.0f;


    public const float initialArmature = 100.0f;
    public const float neighbourThreshold = 0.1f;
    public const float angleThreshold = 0.5f;
    public const float surfaceThreshold = 0.7f;

    public const float FragmentRBodyDrag = 1.5f;
    public const float FragmentRBodyAngDrag = 500.0f;

    // Hinge properties
    public const float FragmentHingeBreakForce = 1.0e4f;
    public const float FragmentHingeMinAngleDeg = 0.0f;
    public const float FragmentHingeMaxAngleDeg = 10.0f;
    public const float FragmentHingeMinDestroyDelay = FragmentMinDestroyDelay * 0.05f;
    public const float FragmentHingeMaxDestroyDelay = FragmentMinDestroyDelay * 0.8f;

    public const float FragmentMinDestroyDelay = 10.0f;
    public const float FragmentMaxDestroyDelay = FragmentMinDestroyDelay * 3.0f;

    public static float DestroyerMiscMinStart = 5.0f;
    public static float DestroyerMiscMaxStart = 6.0f;

    public static float DestroyerBuildMinStart = 40.0f;
    public static float DestroyerBuildMaxStart = 50.0f;
}
