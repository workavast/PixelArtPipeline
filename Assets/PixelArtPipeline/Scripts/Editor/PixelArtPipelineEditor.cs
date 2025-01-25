﻿using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PixelArtPipeline.Editor
{
    /// <summary>
    /// Custom editor for the AnimationCaptureHelper.
    /// </summary>
    [CustomEditor(typeof(PixelArtPipelineCapture))]
    public class PixelArtPipelineEditor : UnityEditor.Editor
    {
        /// <summary>
        /// A message displayed when the target and source clip aren't assigned yet.
        /// </summary>
        private const string ASSIGN_REFS_INFO = "Assign the Target and SourceClip to start previewing!";

        /// <summary>
        /// A message displayed when the capture camera isn't assigned yet.
        /// </summary>
        private const string ASSIGN_CAMERA_INFO = "Assign a camera to start capturing!";

        /// <summary>
        /// The current capture routine in progress.
        /// </summary>
        private IEnumerator _currentCaptureRoutine;

        /// <summary>
        /// Draws the custom inspector for the capture helper.
        /// </summary>
        public override void OnInspectorGUI()
        {
            using (new EditorGUI.DisabledScope(_currentCaptureRoutine != null))
            {
                var helper = (PixelArtPipelineCapture)target;

                var animationCapture = serializedObject.FindProperty("animationCapture");
            
                DrawCaptureOptions(helper);
                DrawAnimationOptions(animationCapture, helper);
            
                serializedObject.ApplyModifiedProperties();
            }
        }
    
        private void DrawCaptureOptions(PixelArtPipelineCapture helper)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Capture Options", EditorStyles.boldLabel);

                var captureCameraProp = serializedObject.FindProperty("captureCamera");
                EditorGUILayout.ObjectField(captureCameraProp, typeof(Camera));

                if (captureCameraProp.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox(ASSIGN_CAMERA_INFO, MessageType.Info);
                    serializedObject.ApplyModifiedProperties();
                    return;
                }

                var resolutionProp = serializedObject.FindProperty("cellSize");
                EditorGUILayout.PropertyField(resolutionProp);
            
                if (GUILayout.Button("Capture Screen"))
                    RunRoutine(helper.CaptureFrame(SaveCapture));
            
                if (GUILayout.Button("Capture Animation"))
                    RunRoutine(helper.CaptureAnimation(SaveCapture));
            }
        }
    
        private void DrawAnimationOptions(SerializedProperty animationCapture, PixelArtPipelineCapture helper)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Animation Options", EditorStyles.boldLabel);
         
                var targetProperty = animationCapture.FindPropertyRelative("target");
                var sourceClipProperty = animationCapture.FindPropertyRelative("sourceClip");
                EditorGUILayout.PropertyField(targetProperty);
                EditorGUILayout.PropertyField(sourceClipProperty);

                if (targetProperty.objectReferenceValue == null
                    || sourceClipProperty.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox(ASSIGN_REFS_INFO, MessageType.Info);
                    serializedObject.ApplyModifiedProperties();
                    return;
                }

                var sourceClip = (AnimationClip)sourceClipProperty.objectReferenceValue;

                var framesPerSecondProperty = animationCapture.FindPropertyRelative("framesPerSecond");
                EditorGUILayout.PropertyField(framesPerSecondProperty);

                var previewFrameProperty = animationCapture.FindPropertyRelative("frameForPreview");
                var lastFrameIndex = (int)(sourceClip.length * framesPerSecondProperty.intValue) - 1;
            
                using (var changeScope = new EditorGUI.ChangeCheckScope())
                {
                    var previewFrame = previewFrameProperty.intValue;
                    previewFrame = Mathf.Clamp(previewFrame, 0, lastFrameIndex);
                    previewFrame = EditorGUILayout.IntSlider("Frame For Preview", previewFrame, 0, lastFrameIndex);

                    if (changeScope.changed)
                    {
                        previewFrameProperty.intValue = previewFrame;
                        helper.AnimationPreview((previewFrame / (float)lastFrameIndex) * sourceClip.length);
                    }
                }

                DrawFramesRangeOptions(animationCapture, lastFrameIndex);
            }
        }

        private static void DrawFramesRangeOptions(SerializedProperty animationCapture, int lastFrameIndex)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var useFramesRangeProperty = animationCapture.FindPropertyRelative("useFramesRange");
                EditorGUILayout.PropertyField(useFramesRangeProperty);

                if (useFramesRangeProperty.boolValue)
                {
                    var startFrameProperty = animationCapture.FindPropertyRelative("startFrame");
                    using (var changeScope = new EditorGUI.ChangeCheckScope())
                    {
                        var frame = startFrameProperty.intValue;
                        frame = Mathf.Clamp(frame, 0, lastFrameIndex);
                        frame = EditorGUILayout.IntSlider("Start Frame", frame, 0, lastFrameIndex);

                        if (changeScope.changed)
                            startFrameProperty.intValue = frame;
                    }
            
                    var endFrameProperty = animationCapture.FindPropertyRelative("endFrame");
                    using (var changeScope = new EditorGUI.ChangeCheckScope())
                    {
                        var frame = endFrameProperty.intValue;
                        frame = Mathf.Clamp(frame, 0, lastFrameIndex);
                        frame = EditorGUILayout.IntSlider("End Frame", frame, 0, lastFrameIndex);

                        if (changeScope.changed)
                            endFrameProperty.intValue = frame;
                    }  
                }
            }
        }
        
        /// <summary>
        /// Saves the captured animation sprite atlases to disk.
        /// </summary>
        private static void SaveCapture(Texture2D diffuseMap, Texture2D normalMap)
        {
            var diffusePath = EditorUtility.SaveFilePanel("Save Capture", "", "NewCapture", "png");

            if (string.IsNullOrEmpty(diffusePath))
                return;

            var fileName = Path.GetFileNameWithoutExtension(diffusePath);
            var directory = Path.GetDirectoryName(diffusePath);
            var normalPath = string.Format("{0}/{1}{2}.{3}", directory, fileName, "NormalMap", "png");

            File.WriteAllBytes(diffusePath, diffuseMap.EncodeToPNG());
            File.WriteAllBytes(normalPath, normalMap.EncodeToPNG());

            AssetDatabase.Refresh();
        }
        
        /// <summary>
        /// Starts running the editor routine.
        /// </summary>
        private void RunRoutine(IEnumerator routine)
        {
            _currentCaptureRoutine = routine;
            EditorApplication.update += UpdateRoutine;
        }

        /// <summary>
        /// Calls MoveNext on the routine each editor frame until the iterator terminates.
        /// </summary>
        private void UpdateRoutine()
        {
            if (!_currentCaptureRoutine.MoveNext())
            {
                EditorApplication.update -= UpdateRoutine;
                _currentCaptureRoutine = null;
            }
        }
    }
}
