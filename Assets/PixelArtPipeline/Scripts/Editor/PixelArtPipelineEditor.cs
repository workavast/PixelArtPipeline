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
         
                var targetProp = animationCapture.FindPropertyRelative("target");
                var sourceClipProp = animationCapture.FindPropertyRelative("sourceClip");
                EditorGUILayout.PropertyField(targetProp);
                EditorGUILayout.PropertyField(sourceClipProp);

                if (targetProp.objectReferenceValue == null
                    || sourceClipProp.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox(ASSIGN_REFS_INFO, MessageType.Info);
                    serializedObject.ApplyModifiedProperties();
                    return;
                }

                var sourceClip = (AnimationClip)sourceClipProp.objectReferenceValue;

                var framesPerSecond = animationCapture.FindPropertyRelative("framesPerSecond");
                EditorGUILayout.PropertyField(framesPerSecond);

                var previewFrameProp = animationCapture.FindPropertyRelative("frameForPreview");
                var framesCount = (int)(sourceClip.length * framesPerSecond.intValue) - 1;
            
                using (var changeScope = new EditorGUI.ChangeCheckScope())
                {
                    var frame = previewFrameProp.intValue;
                    frame = Mathf.Clamp(frame, 0, framesCount);
                    frame = EditorGUILayout.IntSlider("Frame For Preview", frame, 0, framesCount);

                    if (changeScope.changed)
                    {
                        previewFrameProp.intValue = frame;
                        helper.AnimationPreview((frame / (float)framesCount) * sourceClip.length);
                    }
                }
            
            
                var frames = animationCapture.FindPropertyRelative("frameRange");
                EditorGUILayout.PropertyField(frames);

                if (frames.boolValue)
                {
                    var startFrame = animationCapture.FindPropertyRelative("startFrame");
                    using (var changeScope = new EditorGUI.ChangeCheckScope())
                    {
                        var frame = startFrame.intValue;
                        frame = Mathf.Clamp(frame, 0, framesCount);
                        frame = EditorGUILayout.IntSlider("Start Frame", frame, 0, framesCount);

                        if (changeScope.changed)
                            startFrame.intValue = frame;
                    }
            
                    var endFrame = animationCapture.FindPropertyRelative("endFrame");
                    using (var changeScope = new EditorGUI.ChangeCheckScope())
                    {
                        var frame = endFrame.intValue;
                        frame = Mathf.Clamp(frame, 0, framesCount);
                        frame = EditorGUILayout.IntSlider("End Frame", frame, 0, framesCount);

                        if (changeScope.changed)
                            endFrame.intValue = frame;
                    }  
                }
            }
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

        /// <summary>
        /// Saves the captured animation sprite atlases to disk.
        /// </summary>
        private void SaveCapture(Texture2D diffuseMap, Texture2D normalMap)
        {
            var diffusePath = EditorUtility.SaveFilePanel("Save Capture", "", "NewCapture", "png");

            if (string.IsNullOrEmpty(diffusePath))
            {
                return;
            }

            var fileName = Path.GetFileNameWithoutExtension(diffusePath);
            var directory = Path.GetDirectoryName(diffusePath);
            var normalPath = string.Format("{0}/{1}{2}.{3}", directory, fileName, "NormalMap", "png");

            File.WriteAllBytes(diffusePath, diffuseMap.EncodeToPNG());
            File.WriteAllBytes(normalPath, normalMap.EncodeToPNG());

            AssetDatabase.Refresh();
        }
    }
}
