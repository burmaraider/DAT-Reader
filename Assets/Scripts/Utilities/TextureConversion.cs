using UnityEngine;

public static class TextureConversion
{ 
    /// <summary>
    /// Converts a Texture2D to ARGB32.
    /// </summary>
    /// <param name="originalTexture"></param>
    public static Texture2D ConvertTextureToArgb32(Texture2D originalTexture)
    {
        RenderTexture renderTexture = RenderTexture.GetTemporary(
            originalTexture.width,
            originalTexture.height,
            0, // depthBuffer
            RenderTextureFormat.Default,
            RenderTextureReadWrite.sRGB);

        Graphics.Blit(originalTexture, renderTexture);

        Texture2D newTex = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, false, false);
        
        var oldRenderTexture = RenderTexture.active;
        RenderTexture.active = renderTexture;
        newTex.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        newTex.Apply();
        RenderTexture.active = oldRenderTexture;
        RenderTexture.ReleaseTemporary(renderTexture);

        return newTex;
    }
}