using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.Rendering;

public sealed class JPEG_MP4_Compression_Renderer : PostProcessEffectRenderer<JPEG_MP4_Compression>
{
    //The intermediate render targets
    //The Indentifier objects are necessary in order to use the PPSV2 Blit methods
    //lastFrame stores the lastFrame that was rendered
    private RenderTargetIdentifier lastFrameIdentifier;
    private RenderTexture lastFrame;
    //sourceFrame stores the fresh image without any effects on it
    private RenderTargetIdentifier sourceFrameIdentifier;
    private RenderTexture sourceFrame;
    //processedFrame stores the processed/compressed image
    private RenderTargetIdentifier processedFrameIdentifier;
    private RenderTexture processedFrame;

    //The compute shader doing all the work
    private ComputeShader dctShader;

    private void InitRenderTextures(){
        //Calculate the downsampling rate in advance since we will reuse it quite a lot
        int downSamplingRate = Mathf.Max(1, (settings.screenDownsampling + 1));

        //Initialize the lastFrame render target
        lastFrame = new RenderTexture(Screen.width / downSamplingRate, Screen.height / downSamplingRate, 16);
        lastFrame.filterMode = settings.usePointFiltering ? FilterMode.Point : FilterMode.Bilinear;
        lastFrame.Create();
        lastFrameIdentifier = new RenderTargetIdentifier(lastFrame);

        //Initialize the sourceFrame render target
        sourceFrame = new RenderTexture(Screen.width / downSamplingRate, Screen.height / downSamplingRate, 16);
        sourceFrame.filterMode = settings.usePointFiltering ? FilterMode.Point : FilterMode.Bilinear;
        sourceFrame.Create();
        sourceFrameIdentifier = new RenderTargetIdentifier(sourceFrame);

        //Initialize the processed render target
        processedFrame = new RenderTexture(Screen.width / downSamplingRate, Screen.height / downSamplingRate, 16);
        //enableRandomWrite must be enabled since this is the texture the compute shader will write to
        processedFrame.enableRandomWrite = true;
        processedFrame.filterMode = settings.usePointFiltering ? FilterMode.Point : FilterMode.Bilinear;
        processedFrame.Create();
        processedFrameIdentifier = new RenderTargetIdentifier(processedFrame);
    }

    public override void Init(){
        //Load the ComputeShader from Resources
        dctShader = (ComputeShader)Resources.Load("JPEGMP4Compression");

        //Initialize all required render targets
        InitRenderTextures();
    }

    public override void Render(PostProcessRenderContext context){
        //In case any of the render targets gets destroyed, recreate them
        //Usually only happens when quitting the game in the editor
        if(lastFrame == null || sourceFrame == null || processedFrame == null)
            InitRenderTextures();

        //Blit context.source to our sourceFrame RenderTexture so that we can operate on it
        context.command.Blit(context.source, sourceFrameIdentifier);

        //Create the kernel Handle and increment it by one if Fast mode is enabled
        //This way it will then use the second kernel configuration
        int mainKernelHandle = 0;
        if(settings.performanceMode.value == JPEGPerformance.Fast)
            mainKernelHandle = 1;

        //Pass all the RenderTextures to the shader
        dctShader.SetTexture(mainKernelHandle, "Last", lastFrame);
        dctShader.SetTexture(mainKernelHandle, "Input", sourceFrame);
        dctShader.SetTexture(mainKernelHandle, "Result", processedFrame);
        //Pass the user settings to the shader
        dctShader.SetFloat("CompressionThreshold", settings.compressionThreshold);
        dctShader.SetBool("UseBitrate", settings.useBitrate);
        dctShader.SetFloat("Bitrate", settings.bitrate);
        //Dispatch the shader
        //Since each Thread group operates on an 8x8 pixel area we can divide the screen resolution by 8 to determine
        //how many threadgroups are necessary
        //Adding 1 additional threadgroup compensates for sometimes missing edge threadgroups
        dctShader.Dispatch(mainKernelHandle, sourceFrame.width / 8 + 1, sourceFrame.height / 8 + 1, 1);
        //Blit the processed image to the context's destination texture
        context.command.BlitFullscreenTriangle(processedFrameIdentifier, context.destination);
        //In case we're using the MP4 compression the destination needs to be copied to the lastFrame render target
        if(settings.useBitrate)
            context.command.Blit(context.destination, lastFrameIdentifier);
    }

    public override void Release(){
        //Release all render targets
        lastFrame.Release();
        sourceFrame.Release();
        processedFrame.Release();
    }
}