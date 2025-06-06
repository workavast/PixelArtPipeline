using System;
using System.Collections;
using UnityEngine;

namespace Avastrad.PixelArtPipeline
{
    /// <summary>
    /// A component used to help capture screenshot or FBX animations into sprite sheets.
    /// </summary>
    [ExecuteInEditMode]
    public class PixelArtPipelineCapture : MonoBehaviour
    {
        [SerializeField, Tooltip("The camera used to render the animation")]
        private Camera captureCamera = null;

        [SerializeField, Tooltip("The output resolution of the one rendered sprite frame")]
        private Vector2Int cellSize = new Vector2Int(128, 128);
        
        [SerializeField] private bool showDeadZone = true;
        [SerializeField] private bool createNormalMap = true;
        [SerializeField] private AnimationCapture animationCapture;
        [SerializeField] private SingleFrameCapture singleFrameCapture;
        
        private GUIStyle _guiStyle;
        
        private const string VerticalText = "Not\nBe\nCaptured";
        private const string HorizontalText = "Not Be Captured";

        public IEnumerator CaptureAnimation(Action<Texture2D, Texture2D> onComplete)
            => animationCapture.Capture(captureCamera, createNormalMap, cellSize, onComplete);

        public IEnumerator CaptureFrame(Action<Texture2D, Texture2D> onComplete)
            => singleFrameCapture.Capture(captureCamera, createNormalMap, cellSize, onComplete);
    
        public void AnimationPreview(float time)
            => animationCapture.SetAnimationTime(time);

        private void OnValidate()
        {
            var validatedResolution = cellSize;
            validatedResolution.x = Mathf.Clamp(validatedResolution.x, 1, int.MaxValue);
            validatedResolution.y = Mathf.Clamp(validatedResolution.y, 1, int.MaxValue);
            cellSize = validatedResolution;
        }

        private void OnGUI()
        {
            if (showDeadZone)
                DrawDeadZone();
        }
        
        private void DrawDeadZone()
        {
            if (captureCamera == null)
                return;

            _guiStyle ??= new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 64
            };
            
            var cameraWidth = captureCamera.pixelWidth / captureCamera.rect.width;
            var cameraHeight = captureCamera.pixelHeight / captureCamera.rect.height;
            var cameraAspectRatio = cameraWidth / cameraHeight;
            var cellAspectRatio = (float) cellSize.x / cellSize.y;
            
            if (Math.Abs(cameraAspectRatio - cellAspectRatio) > 0.0001f)
            {
                var aspectRatioScale = cellAspectRatio / cameraAspectRatio;
                if (aspectRatioScale <= 1)
                {
                    var captureWithAspect = 1 * aspectRatioScale;
                    GUI.Box(new Rect(
                        0, 0,
                        cameraWidth * (1 - captureWithAspect)/2, cameraHeight), 
                        VerticalText, _guiStyle);
                    GUI.Box(new Rect(
                        cameraWidth * (0.5f + (1 * aspectRatioScale) / 2), 0, 
                        cameraWidth * (1 - captureWithAspect)/2, cameraHeight), 
                        VerticalText, _guiStyle);
                }
                else
                {
                    var captureWithAspect = 1 / aspectRatioScale;
                    GUI.Box(new Rect(
                        0, 0,
                        cameraWidth, cameraHeight * (1 - captureWithAspect)/2), 
                        HorizontalText, _guiStyle);
                    GUI.Box(new Rect(
                        0, cameraHeight * (0.5f + (1 / aspectRatioScale) / 2), 
                        cameraWidth, cameraHeight * (1 - captureWithAspect)/2), 
                        HorizontalText, _guiStyle);
                }
            }
        }
    }
}
