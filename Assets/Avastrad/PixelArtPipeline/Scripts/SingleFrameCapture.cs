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

            if (atlasSize.x > 4096 || atlasSize.y > 4096)
            {
                Debug.LogErrorFormat("Error attempting to capture an animation with a length and" +
                                     "resolution that would produce a texture of size: {0}", atlasSize);
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

            var normalCaptureShader = Shader.Find("Hidden/ViewSpaceNormal");

            captureCamera.targetTexture = rtFrame;
            var cachedCameraColor = captureCamera.backgroundColor;

            try
            {
                yield return null;

                FillFrame(rtFrame, diffuseMap, normalMap, atlasPos, normalCaptureShader, captureCamera);
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