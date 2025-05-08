using System;
using System.Collections;
using UnityEngine;

namespace PixelArtPipeline
{
    public abstract class CaptureBase
    {
        public abstract IEnumerator Capture(Camera captureCamera, Vector2Int cellSize, Action<Texture2D, Texture2D> onComplete);
        
        /// <summary>
        /// Sets all the pixels in the texture to a specified color.
        /// </summary>
        protected static void ClearAtlas(Texture2D texture, Color color)
        {
            var pixels = new Color[texture.width * texture.height];
            for (var i = 0; i < pixels.Length; i++) 
                pixels[i] = color;

            texture.SetPixels(pixels);
            texture.Apply();
        }
        
        protected static Vector2Int CalculateAtlasSize(Vector2Int cellSize, int framesCount, out int columnsCount)
        {
            var framesCountPow = Mathf.CeilToInt(Mathf.Log(framesCount, 2));

            int gridCellCount;
            int newFramesCount;
            if (framesCountPow % 2 == 0)
            {
                newFramesCount = (int)Mathf.Pow(2, framesCountPow);
                gridCellCount = SqrtCeil(newFramesCount);
                columnsCount = gridCellCount;
                return new Vector2Int(cellSize.x * columnsCount, cellSize.y * gridCellCount);
            }

            newFramesCount = (int)Mathf.Pow(2, framesCountPow - 1);
            gridCellCount = SqrtCeil(newFramesCount);
            columnsCount = gridCellCount * 2;
            return new Vector2Int(cellSize.x * columnsCount, cellSize.y * gridCellCount);
        }
        
        protected static void FillFrame(RenderTexture rtFrame, Texture2D diffuseMap, Texture2D normalMap, Vector2Int atlasPos,
            Shader normalCaptureShader, Camera captureCamera)
        {
            captureCamera.backgroundColor = Color.clear;
            captureCamera.Render();
            Graphics.SetRenderTarget(rtFrame);
            diffuseMap.ReadPixels(new Rect(0, 0, rtFrame.width, rtFrame.height), atlasPos.x, atlasPos.y);
            diffuseMap.Apply();

            captureCamera.backgroundColor = new Color(0.5f, 0.5f, 1.0f, 0.0f);
            captureCamera.RenderWithShader(normalCaptureShader, "");
            Graphics.SetRenderTarget(rtFrame);
            normalMap.ReadPixels(new Rect(0, 0, rtFrame.width, rtFrame.height), atlasPos.x, atlasPos.y);
            normalMap.Apply();
        }
        
        /// <summary>
        /// Returns the ceiled square root of the input.
        /// </summary>
        private static int SqrtCeil(int input) 
            => Mathf.CeilToInt(Mathf.Sqrt(input));
    }
}