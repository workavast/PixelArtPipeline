using System;
using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SourceCode.Scripts
{
    [Serializable]
    public class AnimationCapture
    {
        [SerializeField, Tooltip("The target for capturing")]
        private GameObject target;
        
        [SerializeField, Tooltip("The animation clip to capture")]
        private AnimationClip sourceClip ;

        [SerializeField, Min(0), Tooltip("The frame for preview.")]
        private int frameForPreview;

        [SerializeField, Min(1)]
        private int framesPerSecond = 12;
        
        [SerializeField]
        private bool frameRange;
        
        [SerializeField, Min(0)]
        private int startFrame;
        
        [SerializeField, Min(0)]
        private int endFrame;
        
        /// <summary>
        /// Samples the animation clip onto the target object.
        /// </summary>
        public void AnimationPreview(float time)
        {
            if (sourceClip == null || target == null)
            {
                Debug.LogError("SourceClip and Target should be set before animation preview!");
                return;
            }

            sourceClip.SampleAnimation(target, time);
        }

        /// <summary>
        /// Captures the animation as individual frames into a texture.
        /// 
        /// Returns IEnumerator the work can be distributed over multiple editor frame.
        /// This is necessary for SkinnedMeshRenders to update between calls to AnimationClip.Sample()
        /// and Camera.Render(). The provided onComplete action is executed after rendering is finished
        /// so that the textures can be saved to disk.
        /// </summary>
        public IEnumerator CaptureAnimation(Camera captureCamera, Vector2Int cellSize, Action<Texture2D, Texture2D> onComplete)
        {
            if (sourceClip == null || target == null)
            {
                Debug.LogError("CaptureCamera should be set before capturing animation!");
                yield break;
            }

            var fullFramesCount = (int)(sourceClip.length * framesPerSecond);
            if (!frameRange)
            {
                startFrame = 0;
                endFrame = fullFramesCount;
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
            Debug.Log(framesCount);
            var atlasSize = CalculateAtlasSize(cellSize, framesCount, out var columns);
            var atlasPos = new Vector2Int(0, atlasSize.y - cellSize.y);

            if (atlasSize.x > 4096 || atlasSize.y > 4096)
            {
                Debug.LogErrorFormat($"Error attempting to capture an animation with a length and " +
                                     $"resolution that would produce a texture of size: {atlasSize}");
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
                for (var counter = 0; counter < framesCount; counter++)
                {
                    var currentTime = (startFrame + counter / (float)fullFramesCount) * sourceClip.length;
                    AnimationPreview(currentTime);
                    yield return null;

                    FillFrame(rtFrame, diffuseMap, normalMap, atlasPos, normalCaptureShader, captureCamera);

                    atlasPos.x += cellSize.x;

                    if ((counter + 1) % columns == 0)
                    {
                        atlasPos.x = 0;
                        atlasPos.y -= cellSize.y;
                    }
                }

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

        private Vector2Int CalculateAtlasSize(Vector2Int cellSize, int framesCount, out int columnsCount)
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

        private void FillFrame(RenderTexture rtFrame, Texture2D diffuseMap, Texture2D normalMap, Vector2Int atlasPos,
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
        private int SqrtCeil(int input)
        {
            return Mathf.CeilToInt(Mathf.Sqrt(input));
        }

        /// <summary>
        /// Sets all the pixels in the texture to a specified color.
        /// </summary>
        private void ClearAtlas(Texture2D texture, Color color)
        {
            var pixels = new Color[texture.width * texture.height];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            texture.SetPixels(pixels);
            texture.Apply();
        }
    }
}