using UnityEngine;
using UnityEditor;
using DMXlab;
using SimpleJSON;
using System.Collections.Generic;

namespace Klak.DMX
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MatrixOut))]
    public class MatrixOutEditor : Editor
    {
        SerializedProperty _fixture;
        SerializedProperty _pixelIndex;
        SerializedProperty _pixelChannel;

        void OnEnable()
        {
            _fixture = serializedObject.FindProperty("_fixture");
            _pixelIndex = serializedObject.FindProperty("_pixelIndex");
            _pixelChannel = serializedObject.FindProperty("_pixelChannel");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_fixture);

            Fixture fixture = _fixture.objectReferenceValue as Fixture;

            EditorGUILayout.Space();

            if (fixture != null && fixture.useLibrary)
            {
                if (fixture.isMatrix)
                {
                    int pixelIndex = Mathf.Min(_pixelIndex.intValue, fixture.pixelKeys.Count - 1);
                    pixelIndex = EditorGUILayout.Popup("Pixel Key", pixelIndex, fixture.pixelKeys.ToArray());
                    _pixelIndex.intValue = pixelIndex;

                    int templateChannelIndex = Mathf.Min(_pixelChannel.intValue, fixture.templateChannelNames.Count - 1);
                    templateChannelIndex = EditorGUILayout.Popup("Pixel Channel", templateChannelIndex, fixture.templateChannelNames.ToArray());
                    _pixelChannel.intValue = templateChannelIndex;
                }
                else
                    EditorGUILayout.HelpBox("Fixture is not in matrix mode", MessageType.Error);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}