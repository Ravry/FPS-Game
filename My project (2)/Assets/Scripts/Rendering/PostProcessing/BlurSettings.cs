using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[System.Serializable, VolumeComponentMenu("Blur")]
public class BlurSettings : VolumeComponent, IPostProcessComponent
{
    [Tooltip("Blur-Strength")]
    public ClampedFloatParameter strength = new ClampedFloatParameter(0f, 0f, 10f);

    public bool IsActive()
    {
        return (strength.value > 0f) && active;
    }

    public bool IsTileCompatible()
    {
        return false;
    }
}