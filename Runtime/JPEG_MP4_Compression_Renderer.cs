using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

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
    //processedFrame stores the processed/compressed image
    private RenderTargetIdentifier motionFrameIdentifier;
    private RenderTexture motionFrame;

    //The compute shader doing all the work
    private ComputeShader dctShader;
    //The Material used to obtain Unity motion vectors
    private Material motionMaterial;

    //This keeps track of how many B-Frames remain until the next I-Frame
    private int frameIndex;

    private void InitRenderTextures(){
        //Calculate the downsampling rate in advance since we will reuse it quite a lot
        int downSamplingRate = Mathf.Max(1, (settings.screenDownsampling + 1));

        //In case we don't want to render directly to the screen, but to a RenderTexture instead,
        //we need to check if the main cameras targetTexture is not null
        Vector2Int dimensions;
        //targetTexture is null, thus we're rendering to the screen and need to use the screen dimensions
        if(Camera.main.targetTexture == null)
            dimensions = new Vector2Int(Screen.width / downSamplingRate, Screen.height / downSamplingRate);
        //targetTexture is not null, thus we're rendering to a RenderTexture and need to use it's dimensions
        else
            dimensions = new Vector2Int(Camera.main.targetTexture.width / downSamplingRate, 
                                        Camera.main.targetTexture.height / downSamplingRate);

        //Initialize the lastFrame render target
        lastFrame = new RenderTexture(dimensions.x, dimensions.y, 16);
        lastFrame.filterMode = settings.usePointFiltering ? FilterMode.Point : FilterMode.Bilinear;
        lastFrame.Create();
        lastFrameIdentifier = new RenderTargetIdentifier(lastFrame);

        //Initialize the sourceFrame render target
        sourceFrame = new RenderTexture(dimensions.x, dimensions.y, 16);
        sourceFrame.filterMode = settings.usePointFiltering ? FilterMode.Point : FilterMode.Bilinear;
        sourceFrame.Create();
        sourceFrameIdentifier = new RenderTargetIdentifier(sourceFrame);

        //Initialize the processed render target
        processedFrame = new RenderTexture(dimensions.x, dimensions.y, 16);
        //enableRandomWrite must be enabled since this is the texture the compute shader will write to
        processedFrame.enableRandomWrite = true;
        processedFrame.filterMode = settings.usePointFiltering ? FilterMode.Point : FilterMode.Bilinear;
        processedFrame.Create();
        processedFrameIdentifier = new RenderTargetIdentifier(processedFrame);

        //Initialize the motion vector render target
        motionFrame = new RenderTexture(dimensions.x, dimensions.y, 16,
                                        GraphicsFormat.R16G16B16A16_SNorm); //This GraphicsFormat is very important!
                                        //In order to make proper use of the motion vectors we need a render target
                                        //with signed values as the motion vectors are in the range of -1.0 to 1.0
        motionFrame.enableRandomWrite = true;
        motionFrame.filterMode = settings.usePointFiltering ? FilterMode.Point : FilterMode.Bilinear;
        motionFrame.Create();
        motionFrameIdentifier = new RenderTargetIdentifier(motionFrame);
    }

    public override void Init(){
        //Load the ComputeShader from Resources
        dctShader = (ComputeShader)Resources.Load("JPEGMP4Compression");
        motionMaterial = new Material(Shader.Find("Hidden/MotionVectorSource"));

        //Initialize the FrameIndex
        //This will be 0 at first because the first frame should be an I-Frame
        frameIndex = 0;

        //Initialize all required render targets
        InitRenderTextures();

        //Make sure the camera renders motion Vectors
        Camera.main.depthTextureMode = DepthTextureMode.MotionVectors;
    }

    public override void Render(PostProcessRenderContext context){
        //In case any of the render targets gets destroyed, recreate them
        //Usually only happens when quitting the game in the editor
        if(lastFrame == null || sourceFrame == null || processedFrame == null || motionFrame == null)
            InitRenderTextures();

        //Blit context.source to our sourceFrame RenderTexture so that we can operate on it
        context.command.Blit(context.source, sourceFrameIdentifier);

        //Blit the Motion Vectors to our motionFrame render target
        context.command.Blit(motionFrame, motionFrameIdentifier, motionMaterial);

        //Create the kernel Handle and increment it by one if Fast mode is enabled
        //This way it will then use the second kernel configuration
        int mainKernelHandle = 0;
        if(settings.performanceMode.value == JPEGPerformance.Fast)
            mainKernelHandle = 1;

        //Pass all the RenderTextures to the shader
        dctShader.SetTexture(mainKernelHandle, "Last", lastFrame);
        dctShader.SetTexture(mainKernelHandle, "Input", sourceFrame);
        dctShader.SetTexture(mainKernelHandle, "Motion", motionFrame);
        dctShader.SetTexture(mainKernelHandle, "Result", processedFrame);
        //Pass the user settings to the shader
        dctShader.SetBool("UseSpacial", settings.useSpatialCompression);
        dctShader.SetFloat("CompressionThreshold", settings.compressionThreshold);
        dctShader.SetBool("UseTemporal", settings.useTemporalCompression.value && Application.isPlaying);
        dctShader.SetFloat("Bitrate", settings.bitrate);
        dctShader.SetFloat("BitrateArtifacts", settings.bitrateArtifacts);
        //If there are no B-Frames remaining the next frame will be an I-Frame
        //Motion Vectors do not work in the editor window, so we have to make sure that this only takes effect when
        //the game is running
        if(frameIndex == 0 || !Application.isPlaying){
            dctShader.SetBool("IsIFrame", true);
            frameIndex = Mathf.Max(settings.numBFrames, 0);
        }
        //Otherwise just render another B-Frame
        else{
            dctShader.SetBool("IsIFrame", false);
            //A B-Frame was rendered so we decrease the FrameIndex
            //Only if we want to actually use I-Frames though
            if(settings.useIFrames) frameIndex--;
        }
        //Dispatch the shader
        //Since each Thread group operates on an 8x8 pixel area we can divide the screen resolution by 8 to determine
        //how many threadgroups are necessary
        //Adding 7 before dividing is some integer magic that prevents potential spawning of offscreen threads
        dctShader.Dispatch(mainKernelHandle, (sourceFrame.width + 7) / 8, (sourceFrame.height + 7) / 8, 1);
        //Blit the processed image to the context's destination texture
        context.command.BlitFullscreenTriangle(processedFrameIdentifier, context.destination);
        //In case we're using the MP4 compression the destination needs to be copied to the lastFrame render target
        context.command.Blit(context.destination, lastFrameIdentifier);
    }

    public override void Release(){
        //Release all render targets
        lastFrame.Release();
        sourceFrame.Release();
        processedFrame.Release();
        motionFrame.Release();
    }
}