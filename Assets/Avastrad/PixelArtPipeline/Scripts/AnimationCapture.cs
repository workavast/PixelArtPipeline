using System;
using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Avastrad.PixelArtPipeline
{
    [Serializable]
    public class AnimationCapture : CaptureBase
    {
        [SerializeField, Tooltip("The target for capturing")]
        private GameObject target;
        
        [SerializeField, Tooltip("The animation clip to capture")]
        private AnimationClip sourceClip ;

        [SerializeField, Min(0), Tooltip("The frame for preview")]
        private int frameForPreview;//used in the editor class, dont delete it

        [SerializeField, Min(1)]
        private int framesPerSecond = 12;
        
        [SerializeField]
        private bool useFramesRange;
        
        [SerializeField, Min(0)]
        private int startFrame;
        
        [SerializeField, Min(0)]
        private int endFrame;

        /// <summary>
        /// Captures the animation as individual frames into a texture.
        /// 
        /// Returns IEnumerator the work can be distributed over multiple editor frame.
        /// This is necessary for SkinnedMeshRenders to update between calls to AnimationClip.Sample()
        /// and Camera.Render(). The provided onComplete action is executed after rendering is finished
        /// so that the textures can be saved to disk.
        /// </summary>
        public override IEnumerator Capture(Camera captureCamera, bool createNormalMap, Vector2Int cellSize, Action<Texture2D, Texture2D> onComplete)
        {
            if (sourceClip == null || target == null)
            {
                Debug.LogError("CaptureCamera and target should be set before capturing animation!");
                yield break;
            }

            var fullFramesCount = (int)(sourceClip.length * framesPerSecond);
            if (!useFramesRange)
            {
                startFrame = 0;
                endFrame = fullFramesCount - 1;
            }
            else
            {
                if (startFrame > endFrame)
                {
                    Debug.LogError($"Start frame cant be larger than the end frame");
                    yield break;
                }
            }
            
            var framesCount = endFrame - startFrame + 1;
            var atlasSize = CalculateAtlasSize(cellSize, framesCount, out var columns);
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
                var atlasFramePosition = new Vector2Int(0, atlasSize.y - cellSize.y);
                for (var frameIndex = 0; frameIndex < framesCount; frameIndex++)
                {
                    var currentTime = ((startFrame + frameIndex) / (float)(fullFramesCount - 1)) * sourceClip.length;
                    SetAnimationTime(currentTime);
                    
                    yield return null;
                    
                    RenderMaps(rtFrame, diffuseMap, normalMap, atlasFramePosition, captureCamera);

                    atlasFramePosition.x += cellSize.x;

                    if ((frameIndex + 1) % columns == 0)
                    {
                        atlasFramePosition.x = 0;
                        atlasFramePosition.y -= cellSize.y;
                    }
                }

                onComplete.Invoke(diffuseMap, normalMap);
            }
            finally
            {
                restoreCameraAction?.Invoke();
                Graphics.SetRenderTarget(null);
                Object.DestroyImmediate(rtFrame);
            }
        }
        
        /// <summary>
        /// Samples the animation clip onto the target object.
        /// </summary>
        public void SetAnimationTime(float time)
        {
            if (sourceClip == null || target == null)
            {
                Debug.LogError("SourceClip and Target should be set before animation preview!");
                return;
            }

            sourceClip.SampleAnimation(target, time);
        }
    }
}