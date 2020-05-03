using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

//A custom enum parameter determining the performance mode
public enum JPEGPerformance{
    Accurate = 0,
    Fast = 1
}
[System.Serializable]
public sealed class JPEGPerformanceParameter : ParameterOverride<JPEGPerformance>{}

[System.Serializable]
[PostProcess(typeof(JPEG_MP4_Compression_Renderer), PostProcessEvent.AfterStack, "Custom/JPEG\\MP4 Compression")]
public sealed class JPEG_MP4_Compression : PostProcessEffectSettings
{
    //screenDownsampling determines by which number + 1 the screen resolution should be divided
    //Can help increase performance or simply create an even lower quality look
    [Tooltip("Updates on Play")]
    public IntParameter screenDownsampling = new IntParameter { value = 0 };
    //usePointFiltering enables or disables point filtering for all RenderTextures used behind the scenes
    [Tooltip("Updates on Play")]
    public BoolParameter usePointFiltering = new BoolParameter { value = false };
    //compressionThreshold determines how intensely the image should be compressed
    //0.0 is no compression
    //2.0 is extreme compression
    [Range(0.0f, 2.0f), Tooltip("Compression Threshold")]
    public FloatParameter compressionThreshold = new FloatParameter { value = 0.0f };
    //performanceMode can help increase the performance
    //Accurate will compress all 3 color channels using DCT
    //Fast will only compress the luminance channel with DCT and the chrominance channels with simple quantization
    [Tooltip("Performance Mode")]
    public JPEGPerformanceParameter performanceMode = new JPEGPerformanceParameter { value = JPEGPerformance.Accurate };
    //These Parameters are still WIP and will not look accurate
    //useBitrate enables or disables the MP4 interframe compression
    [Tooltip("Use Bitrate")]
    public BoolParameter useBitrate = new BoolParameter { value = false };
    //bitrate controls how many pixelBlocks lag behind on each frame
    //0.0 is none
    //1.0 is a lot
    [Range(0.0f, 1.0f), Tooltip("Bitrate")]
    public FloatParameter bitrate = new FloatParameter { value = 1.0f };
}