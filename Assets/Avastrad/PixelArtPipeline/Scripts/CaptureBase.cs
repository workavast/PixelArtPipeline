using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Avastrad.PixelArtPipeline
{
    public abstract class CaptureBase
    {
        public abstract IEnumerator Capture(Camera captureCamera, Vector2Int cellSize, Action<Texture2D, Texture2D> onComplete);

        protected static Action PrepareCamera(Camera captureCamera, Vector2Int cellSize)
        {
            var cameraAspect = captureCamera.pixelWidth / captureCamera.pixelHeight;
            var targetAspect = (float)cellSize.x / cellSize.y;
            if (targetAspect <= cameraAspect)
                return null;

            if (captureCamera.orthographic)
            {
                var originalOrthoSize = captureCamera.orthographicSize;
                var originalAspect = captureCamera.aspect;
                
                var targetOrthoSize = originalOrthoSize * (originalAspect / targetAspect);
                captureCamera.orthographicSize = targetOrthoSize;

                return () => { captureCamera.orthographicSize = originalOrthoSize; };
            }
            else
            {
                var originalHorFov = Camera.VerticalToHorizontalFieldOfView(captureCamera.fieldOfView, captureCamera.aspect);
                captureCamera.fieldOfView = Camera.HorizontalToVerticalFieldOfView(originalHorFov, targetAspect);
                
                return () => { captureCamera.fieldOfView = Camera.HorizontalToVerticalFieldOfView(originalHorFov, captureCamera.aspect); };
            }
        }
        
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

        protected static void FillFrame(RenderTexture rtFrame, Texture2D diffuseMap, Texture2D normalMap,
            Vector2Int atlasPos, Camera captureCamera)
        {
            RenderDiffuseMap(rtFrame, diffuseMap, atlasPos, captureCamera);

            var pipelineAsset = GraphicsSettings.renderPipelineAsset;
            if (pipelineAsset == null)
                RenderNormalMap_BuiltIn(rtFrame, normalMap, atlasPos, captureCamera);
            else if (pipelineAsset.GetType().ToString().Contains("UniversalRenderPipelineAsset"))
                RenderNormalMap_URP(rtFrame, normalMap, atlasPos, captureCamera);
            else
                Debug.LogError("Undefined render pipeline");
        }

        private static void RenderDiffuseMap(RenderTexture rtFrame, Texture2D diffuseMap, Vector2Int atlasPos,
            Camera captureCamera)
        {
            captureCamera.backgroundColor = Color.clear;
            captureCamera.Render();
            Graphics.SetRenderTarget(rtFrame);
            diffuseMap.ReadPixels(new Rect(0, 0, rtFrame.width, rtFrame.height), atlasPos.x, atlasPos.y);
            diffuseMap.Apply();
        }

        private static void RenderNormalMap_BuiltIn(RenderTexture rtFrame, Texture2D normalMap, Vector2Int atlasPos,
            Camera captureCamera)
        {
            var normalShader = Shader.Find("Hidden/ViewSpaceNormal_BuiltIn");
            if (normalShader == null)
                throw new NullReferenceException("Cant find shader: Hidden/ViewSpaceNormal_BuiltIn");

            captureCamera.backgroundColor = new Color(0.5f, 0.5f, 1.0f, 0.0f);
            captureCamera.RenderWithShader(normalShader, "");
            Graphics.SetRenderTarget(rtFrame);
            normalMap.ReadPixels(new Rect(0, 0, rtFrame.width, rtFrame.height), atlasPos.x, atlasPos.y);
            normalMap.Apply();
        }

        protected static void RenderNormalMap_URP(RenderTexture rtFrame, Texture2D normalMap,
            Vector2Int atlasPos, Camera captureCamera)
        {
            var normalShader = Shader.Find("Hidden/ViewSpaceNormal_URP");
            if (normalShader == null)
                throw new NullReferenceException("Cant find shader: Hidden/ViewSpaceNormal_URP");

            var originalMaterials = new Dictionary<Renderer, Material[]>();
            var allRenderers = UnityEngine.Object.FindObjectsOfType<Renderer>();
            foreach (var renderer in allRenderers)
            {
                originalMaterials[renderer] = renderer.sharedMaterials;
                var materials = new Material[renderer.sharedMaterials.Length];
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = new Material(normalShader);
                }

                renderer.sharedMaterials = materials;
            }

            captureCamera.backgroundColor = new Color(0.5f, 0.5f, 1.0f, 0.0f);
            captureCamera.targetTexture = rtFrame;
            captureCamera.Render();
            Graphics.SetRenderTarget(rtFrame);
            normalMap.ReadPixels(new Rect(0, 0, rtFrame.width, rtFrame.height), atlasPos.x, atlasPos.y);
            normalMap.Apply();

            foreach (var kvp in originalMaterials)
                kvp.Key.sharedMaterials = kvp.Value;
        }

        /// <summary>
        /// Returns the ceil-ed square root of the input.
        /// </summary>
        private static int SqrtCeil(int input)
            => Mathf.CeilToInt(Mathf.Sqrt(input));
    }
}

