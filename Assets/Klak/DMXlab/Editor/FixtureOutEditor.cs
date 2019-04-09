using UnityEngine;
using UnityEditor;
using DMXlab;
using SimpleJSON;
using System.Collections.Generic;

namespace Klak.DMX
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(FixtureOut))]
    public class FixtureOutEditor : Editor
    {
        SerializedProperty _fixture;
        SerializedProperty _channel;
        SerializedProperty _mode;
        SerializedProperty _capabilityName;
        SerializedProperty _pixelIndex;
        SerializedProperty _pixelChannel;

        void OnEnable()
        {
            _fixture = serializedObject.FindProperty("_fixture");
            _channel = serializedObject.FindProperty("_channel");
            _mode = serializedObject.FindProperty("_mode");
            _capabilityName = serializedObject.FindProperty("_capabilityName");
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
                EditorGUILayout.PropertyField(_mode);

                EditorGUILayout.Space();

                if (_mode.enumValueIndex == (int)FixtureOut.Mode.Capability)
                {
                    List<string> capabilities = new List<string>(fixture.capabilityNames);
                    int numCapabilities = capabilities.Count;

                    int capabilityIndex = 0;
                    string capabilityName = _capabilityName.stringValue;
                    if (!string.IsNullOrEmpty(capabilityName))
                    {
                        capabilityIndex = capabilities.IndexOf(capabilityName);
                        if (capabilityIndex == -1)
                        {
                            capabilities.Add(_capabilityName.stringValue + " (not available)");
                            capabilityIndex = numCapabilities;
                        }
                    }

                    capabilityIndex = EditorGUILayout.Popup("Capability", capabilityIndex, capabilities.ToArray());
                    if (capabilityIndex < numCapabilities)
                        _capabilityName.stringValue = capabilities[capabilityIndex];

                    _channel.intValue = fixture.GetCapabilityChannelIndex(_capabilityName.stringValue);
                }
                else if (_mode.enumValueIndex == (int)FixtureOut.Mode.Matrix)
                {
                    if (fixture.isMatrix)
                    {
                        int pixelIndex = Mathf.Min(_pixelIndex.intValue, fixture.pixelKeys.Count - 1);
                        pixelIndex = EditorGUILayout.Popup("Pixel Key", pixelIndex, fixture.pixelKeys.ToArray());
                        _pixelIndex.intValue = pixelIndex;

                        int templateChannelIndex = Mathf.Min(_pixelChannel.intValue, fixture.templateChannelNames.Count - 1);
                        templateChannelIndex = EditorGUILayout.Popup("Pixel Channel", templateChannelIndex, fixture.templateChannelNames.ToArray());
                        _pixelChannel.intValue = templateChannelIndex;

                        string channelName = FixtureLibrary.ExpandTemplateChannelName(fixture.templateChannelNames[templateChannelIndex], fixture.pixelKeys[pixelIndex]);
                        _channel.intValue = fixture.channelNames.IndexOf(channelName);
                    }
                    else
                        EditorGUILayout.HelpBox("Fixture is not in matrix mode", MessageType.Error);
                }
                else
                {

                    int channelIndex = Mathf.Min(_channel.intValue, fixture.numChannels - 1);
                    channelIndex = EditorGUILayout.Popup("Channel", channelIndex, fixture.channelNames.ToArray());
                    _channel.intValue = channelIndex;
                }
            }
            else
                EditorGUILayout.PropertyField(_channel);

            serializedObject.ApplyModifiedProperties();
        }
    }
}