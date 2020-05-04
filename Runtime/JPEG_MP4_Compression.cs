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
    [Header("Spatial Compression")]
    //useSpacialCompression controls if the classic JPEG compression should be applied
    [Tooltip("Use Spatial Compression")]
    public BoolParameter useSpatialCompression = new BoolParameter { value = true };
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

    //Temporal Compression only works when the game is actually running since motion vectors are not available otherwise
    [Header("Temporal Compression (Playmode Only)")]
    //useTemporalCompression controls wether to apply Interframe compression effects
    [Tooltip("Use Temporal Compression")]
    public BoolParameter useTemporalCompression = new BoolParameter { value = false };
    //I-Frames force a complete screen update every few frames(gap depending on the number of B-Frames)
    //For certain effects like Datamoshing you might want to disable them completely though
    [Tooltip("Use I-Frames")]
    public BoolParameter useIFrames = new BoolParameter { value = true };
    //B-Frames try to predict the look of the next frame by moving tiny parts of the previous frame around
    //The more B-Frames you use the more noticable the effect
    [Tooltip("Number of predicted frames")]
    public IntParameter numBFrames = new IntParameter { value = 8};
    //bitrate controls how many pixelBlocks lag behind on each frame
    //1.0 is none
    //0.0 is a lot
    //For reasonable effects you should keep it around 0.9 
    //I won't stop you from going crazy with it though
    [Range(0.0f, 1.0f), Tooltip("Bitrate")]
    public FloatParameter bitrate = new FloatParameter { value = 1.0f };
    //When a pixelBlock is updated it might still leave behind some artifacts of it's previous contents
    //Use bitrateArtifacts to control how much should bleed through
    [Range(0.0f, 0.95f)]
    public FloatParameter bitrateArtifacts = new FloatParameter { value = 0.0f };
}