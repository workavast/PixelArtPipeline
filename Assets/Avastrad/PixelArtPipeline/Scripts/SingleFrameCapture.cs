using System;
using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Avastrad.PixelArtPipeline
{
    [Serializable]
    internal class SingleFrameCapture : CaptureBase
    {
        public override IEnumerator Capture(Camera captureCamera, bool createNormalMap, Vector2Int cellSize, Action<Texture2D, Texture2D> onComplete)
        {
            var atlasSize = CalculateAtlasSize(cellSize, 1, out _);
            if (atlasSize.x > 8192 || atlasSize.y > 8192)
            {
                Debug.LogError($"If atlas resolution higher then 8192, can happened OutOfMemoryException. " +
                               $"Current resolution is {atlasSize}");
                yield break;
            }

            var diffuseMap = CreateDiffuseMap(atlasSize);
            var normalMap = createNormalMap ? CreateNormalMap(atlasSize) : null;
            var rtFrame = CreateRenderTextureFrame(cellSize);
            
            var restoreCameraAction = PrepareCamera(captureCamera, cellSize, rtFrame);

            try
            {
                yield return null;

                var atlasFramePosition = new Vector2Int(0, atlasSize.y - cellSize.y);
                RenderMaps(rtFrame, diffuseMap, normalMap, atlasFramePosition, captureCamera);
                
                onComplete.Invoke(diffuseMap, normalMap);
            }
            finally
            {
                restoreCameraAction?.Invoke();
                Graphics.SetRenderTarget(null);
                Object.DestroyImmediate(rtFrame);
            }
        }
    }
}