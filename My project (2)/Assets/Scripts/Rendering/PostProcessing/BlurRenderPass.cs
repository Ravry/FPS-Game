using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

public class BlurRenderPass : ScriptableRenderPass
{
    private Material material;
    private BlurSettings settings;
    private RenderTargetIdentifier source;
    private RenderTargetHandle blurTex;
    private int blurTexID;

    public bool Setup(ScriptableRenderer renderer)
    {
        source = renderer.cameraColorTarget;
        settings = VolumeManager.instance.stack.GetComponent<BlurSettings>();
        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

        if (settings != null && settings.IsActive())
        {
            material = new Material(Shader.Find("PostProcessing/Blur"));
            return true;
        }

        return false;
    }

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        if (settings == null || !settings.IsActive())
        {
            return;
        }

        blurTexID = Shader.PropertyToID("_BlurTex");
        blurTex = new RenderTargetHandle();
        blurTex.id = blurTexID;
        cmd.GetTemporaryRT(blurTex.id, cameraTextureDescriptor);
        base.Configure(cmd, cameraTextureDescriptor);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (settings == null || !settings.IsActive())
        {
            return;
        }

        CommandBuffer cmd = CommandBufferPool.Get("Blur Post Process");

        int gridSize = Mathf.CeilToInt(settings.strength.value * 6f);

        if (gridSize % 2 == 0)
        {
            gridSize++;
        }

        material.SetInteger("_GridSize", gridSize);
        material.SetFloat("_Spread", settings.strength.value);

        cmd.Blit(source, blurTex.id, material, 0);
        cmd.Blit(blurTex.id, source, material, 1);

        context.ExecuteCommandBuffer(cmd);
        
        
        cmd.Clear();
        CommandBufferPool.Release(cmd);
    }

    public override void FrameCleanup(CommandBuffer cmd)
    {
        cmd.ReleaseTemporaryRT(blurTexID);
        base.FrameCleanup(cmd);
    }
}