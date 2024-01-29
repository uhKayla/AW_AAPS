// #if UNITY_EDITOR
// using System;
// using System.Collections.Generic;
// using UnityEditor;
// using UnityEngine;
// using VRC.SDKBase;
// using UnityEngine.UIElements;
// using UnityEditor.UIElements;
//
// namespace ANGELWARE.AW_AAPS.Editor
// {
//     [CustomEditor(typeof(AW_AAPS))]
//     public class AW_MergeDbtsEditor : AW_BaseInspector, IEditorOnly
//     {
//         private SerializedProperty _apsMarkers;
//         private SerializedProperty _oneFloat;
//
//
//         private void OnEnable()
//         {
//             _apsMarkers = serializedObject.FindProperty("holeMarkers");
//             _oneFloat = serializedObject.FindProperty("oneFloatParameter");
//         }
//
//         protected override void SetupContent(VisualElement root)
//         {
//             var container = root.Q<VisualElement>("Container");
//             
//             var of = new PropertyField(_oneFloat);
//             container.Add(of);
//
//             var s = new PropertyField(_apsMarkers);
//             s.style.paddingLeft = 7;
//             container.Add(s);
//
//         }
//     }
// }
//
// #endif