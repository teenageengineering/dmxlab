using UnityEngine;
using UnityEditor;
using DMXlab;
using SimpleJSON;
using System.Collections.Generic;

namespace Klak.DMX
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(FixtureColorOut))]
    public class FixtureColorOutEditor : Editor
    {
        SerializedProperty _fixture;
        SerializedProperty _redChannel;
        SerializedProperty _greenChannel;
        SerializedProperty _blueChannel;
        SerializedProperty _mode;
        SerializedProperty _pixelIndex;

        void OnEnable()
        {
            _fixture = serializedObject.FindProperty("_fixture");
            _redChannel = serializedObject.FindProperty("_redChannel");
            _greenChannel = serializedObject.FindProperty("_greenChannel");
            _blueChannel = serializedObject.FindProperty("_blueChannel");
            _mode = serializedObject.FindProperty("_mode");
            _pixelIndex = serializedObject.FindProperty("_pixelIndex");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_fixture);

            Fixture fixture = _fixture.objectReferenceValue as Fixture;

            EditorGUILayout.Space();

            if (fixture != null && fixture.useLibrary)
            {
                EditorGUILayout.PropertyField(_mode);

                EditorGUILayout.Space();

                if (_mode.enumValueIndex == (int)FixtureColorOut.Mode.Matrix)
                {
                    if (fixture.isMatrix)
                    {
                        int pixelIndex = Mathf.Min(_pixelIndex.intValue, fixture.pixelKeys.Count - 1);
                        pixelIndex = EditorGUILayout.Popup("Pixel Key", pixelIndex, fixture.pixelKeys.ToArray());
                        _pixelIndex.intValue = pixelIndex;

                        // TODO: look for color intestity template channels
                    }
                    else
                        EditorGUILayout.HelpBox("Fixture is not in matrix mode", MessageType.Error);
                }
                else 
                {
                    if (fixture.colorChannels.Count == 0)
                    {
                        _redChannel.intValue = _greenChannel.intValue = _blueChannel.intValue = -1;
                        EditorGUILayout.HelpBox("Fixture is missing color capabilities", MessageType.Error);
                    }
                    else
                    {
                        _redChannel.intValue = (fixture.colorChannels.ContainsKey("Red")) ? fixture.colorChannels["Red"] : -1;
                        _greenChannel.intValue = (fixture.colorChannels.ContainsKey("Green")) ? fixture.colorChannels["Green"] : -1;
                        _blueChannel.intValue = (fixture.colorChannels.ContainsKey("Blue")) ? fixture.colorChannels["Blue"] : -1;
                    }
                }
            }
            else
            {
                EditorGUILayout.PropertyField(_redChannel);
                EditorGUILayout.PropertyField(_greenChannel);
                EditorGUILayout.PropertyField(_blueChannel);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}