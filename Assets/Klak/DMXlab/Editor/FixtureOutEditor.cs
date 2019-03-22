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
        SerializedProperty _selectCapability;
        SerializedProperty _capabilityName;

        void OnEnable()
        {
            _fixture = serializedObject.FindProperty("_fixture");
            _channel = serializedObject.FindProperty("_channel");
            _selectCapability = serializedObject.FindProperty("_selectCapability");
            _capabilityName = serializedObject.FindProperty("_capabilityName");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_fixture);

            Fixture fixture = _fixture.objectReferenceValue as Fixture;

            EditorGUILayout.Space();

            if (fixture != null && fixture.useLibrary)
            {
                EditorGUILayout.PropertyField(_selectCapability);

                if (_selectCapability.boolValue)
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