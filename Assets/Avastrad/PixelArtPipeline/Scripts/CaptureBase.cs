using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Avastrad.PixelArtPipeline
{
    internal abstract class CaptureBase
    {
        public abstract IEnumerator Capture(Camera captureCamera, bool createNormalMap, Vector2Int cellSize, Action<Texture2D, Texture2D> onComplete);

        protected static Texture2D CreateDiffuseMap(Vector2Int atlasSize)
        {
            var diffuseMap = new Texture2D(atlasSize.x, atlasSize.y, TextureFormat.ARGB32, false)
            {
                filterMode = FilterMode.Point
            };
            FillTexture(diffuseMap, Color.clear);
            return diffuseMap;
        }
        
        protected static Texture2D CreateNormalMap(Vector2Int atlasSize)
        {
            var normalMap = new Texture2D(atlasSize.x, atlasSize.y, TextureFormat.ARGB32, false)
            {
                filterMode = FilterMode.Point
            };
            FillTexture(normalMap, new Color(0.5f, 0.5f, 1.0f, 0.0f));
            return normalMap;
        }
        
        protected static RenderTexture CreateRenderTextureFrame(Vector2Int cellSize)
        {
            return new RenderTexture(cellSize.x, cellSize.y, 24, RenderTextureFormat.ARGB32)
            {
                filterMode = FilterMode.Point,
                antiAliasing = 1,
                hideFlags = HideFlags.HideAndDontSave
            };
        }
        
        protected static Action PrepareCamera(Camera captureCamera, Vector2Int cellSize, RenderTexture rtFrame)
        {
            var cameraAspect = captureCamera.pixelWidth / captureCamera.pixelHeight;
            var targetAspect = (float)cellSize.x / cellSize.y;
            if (targetAspect <= cameraAspect)
            {
                var originalCameraColor = captureCamera.backgroundColor;
                captureCamera.targetTexture = rtFrame;
                return () =>
                {
                    captureCamera.targetTexture = null;
                    captureCamera.backgroundColor = originalCameraColor;
                };
            }

            if (captureCamera.orthographic)
            {
                var originalOrthoSize = captureCamera.orthographicSize;
                var originalAspect = captureCamera.aspect;
                
                var targetOrthoSize = originalOrthoSize * (originalAspect / targetAspect);
                captureCamera.orthographicSize = targetOrthoSize;

                var originalCameraColor = captureCamera.backgroundColor;
                captureCamera.targetTexture = rtFrame;
                
                return () =>
                {
                    captureCamera.targetTexture = null;
                    captureCamera.backgroundColor = originalCameraColor;
                    captureCamera.orthographicSize = originalOrthoSize;
                };
            }
            else
            {
                var originalHorFov = Camera.VerticalToHorizontalFieldOfView(captureCamera.fieldOfView, captureCamera.aspect);
                captureCamera.fieldOfView = Camera.HorizontalToVerticalFieldOfView(originalHorFov, targetAspect);

                var originalCameraColor = captureCamera.backgroundColor;
                captureCamera.targetTexture = rtFrame;
                
                return () =>
                {
                    captureCamera.targetTexture = null;
                    captureCamera.backgroundColor = originalCameraColor;
                    captureCamera.fieldOfView = Camera.HorizontalToVerticalFieldOfView(originalHorFov, captureCamera.aspect);
                };
            }
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

        protected static void RenderMaps(RenderTexture rtFrame, Texture2D diffuseMap, Texture2D normalMap,
            Vector2Int atlasPos, Camera captureCamera)
        {
            RenderDiffuseMap(rtFrame, diffuseMap, atlasPos, captureCamera);
            if (normalMap != null)
                RenderNormalMap(rtFrame, normalMap, atlasPos, captureCamera);
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
        
        private static void RenderNormalMap(RenderTexture rtFrame, Texture2D normalMap, Vector2Int atlasPos,
            Camera captureCamera)
        {
            var pipelineAsset = GraphicsSettings.renderPipelineAsset;
            if (pipelineAsset == null)
                RenderNormalMap_BuiltIn(rtFrame, normalMap, atlasPos, captureCamera);
            else if (pipelineAsset.GetType().ToString().Contains("UniversalRenderPipelineAsset"))
                RenderNormalMap_URP(rtFrame, normalMap, atlasPos, captureCamera);
            else
                Debug.LogError("Undefined render pipeline");   
        }
        
        private static void RenderNormalMap_BuiltIn(RenderTexture rtFrame, Texture2D normalMap, Vector2Int atlasPos,
            Camera captureCamera)
        {
            const string shader = "Hidden/ViewSpaceNormal_BuiltIn";
            var normalShader = Shader.Find(shader);
            if (normalShader == null)
                throw new NullReferenceException($"Cant find shader: {shader}");

            captureCamera.backgroundColor = new Color(0.5f, 0.5f, 1.0f, 0.0f);
            captureCamera.RenderWithShader(normalShader, "");
            Graphics.SetRenderTarget(rtFrame);
            normalMap.ReadPixels(new Rect(0, 0, rtFrame.width, rtFrame.height), atlasPos.x, atlasPos.y);
            normalMap.Apply();
        }

        private static void RenderNormalMap_URP(RenderTexture rtFrame, Texture2D normalMap,
            Vector2Int atlasPos, Camera captureCamera)
        {
            const string shader = "Hidden/ViewSpaceNormal_URP";
            var normalShader = Shader.Find(shader);
            if (normalShader == null)
                throw new NullReferenceException($"Cant find shader: {shader}");

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
        /// Sets all the pixels in the texture to a specified color.
        /// </summary>
        private static void FillTexture(Texture2D texture, Color color)
        {
            var pixels = new Color[texture.width * texture.height];
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = color;

            texture.SetPixels(pixels);
            texture.Apply();
        }
        
        /// <summary>
        /// Returns the ceil-ed square root of the input.
        /// </summary>
        private static int SqrtCeil(int input)
            => Mathf.CeilToInt(Mathf.Sqrt(input));
    }
}

