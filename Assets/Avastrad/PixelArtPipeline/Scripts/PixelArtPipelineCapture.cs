using System;
using System.Collections;
using UnityEngine;

namespace PixelArtPipeline
{
    /// <summary>
    /// A component used to help capture screenshot or FBX animations into sprite sheets.
    /// </summary>
    public class PixelArtPipelineCapture : MonoBehaviour
    {
        [SerializeField]
        private AnimationCapture animationCapture;

        [SerializeField] 
        private SingleFrameCapture singleFrameCapture;
    
        [SerializeField, Tooltip("The camera used to render the animation.")]
        private Camera captureCamera = null;

        [SerializeField, Tooltip("The output resolution of the one rendered sprite frame.")]
        private Vector2Int cellSize = new Vector2Int(128, 128);
    
        public IEnumerator CaptureAnimation(Action<Texture2D, Texture2D> onComplete)
            => animationCapture.Capture(captureCamera, cellSize, onComplete);

        public IEnumerator CaptureFrame(Action<Texture2D, Texture2D> onComplete)
            => singleFrameCapture.Capture(captureCamera, cellSize, onComplete);
    
        public void AnimationPreview(float time)
            => animationCapture.AnimationPreview(time);
    }
}
