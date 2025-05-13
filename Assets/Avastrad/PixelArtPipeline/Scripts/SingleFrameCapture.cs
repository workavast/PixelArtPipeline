using System;
using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Avastrad.PixelArtPipeline
{
    [Serializable]
    public class SingleFrameCapture : CaptureBase
    {
        public override IEnumerator Capture(Camera captureCamera, Vector2Int cellSize, Action<Texture2D, Texture2D> onComplete)
        {
            var atlasSize = CalculateAtlasSize(cellSize, 1, out var columns);
            var atlasPos = new Vector2Int(0, atlasSize.y - cellSize.y);

            if (atlasSize.x > 8192 || atlasSize.y > 8192)
            {
                Debug.LogError($"If atlas resolution higher then 8192, can happened OutOfMemoryException. " +
                               $"Current resolution is {atlasSize}");
                yield break;
            }

            var diffuseMap = new Texture2D(atlasSize.x, atlasSize.y, TextureFormat.ARGB32, false)
            {
                filterMode = FilterMode.Point
            };
            ClearAtlas(diffuseMap, Color.clear);

            var normalMap = new Texture2D(atlasSize.x, atlasSize.y, TextureFormat.ARGB32, false)
            {
                filterMode = FilterMode.Point
            };
            ClearAtlas(normalMap, new Color(0.5f, 0.5f, 1.0f, 0.0f));

            var rtFrame = new RenderTexture(cellSize.x, cellSize.y, 24, RenderTextureFormat.ARGB32)
            {
                filterMode = FilterMode.Point,
                antiAliasing = 1,
                hideFlags = HideFlags.HideAndDontSave
            };
            
            captureCamera.targetTexture = rtFrame;
            var cachedCameraColor = captureCamera.backgroundColor;

            try
            {
                yield return null;

                FillFrame(rtFrame, diffuseMap, normalMap, atlasPos, captureCamera);
                onComplete.Invoke(diffuseMap, normalMap);
            }
            finally
            {
                Graphics.SetRenderTarget(null);
                captureCamera.targetTexture = null;
                captureCamera.backgroundColor = cachedCameraColor;
                Object.DestroyImmediate(rtFrame);
            }
        }
    }
}