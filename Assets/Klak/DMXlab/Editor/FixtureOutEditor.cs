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
            if (fixture != null && fixture.useLibrary)
            {
                EditorGUILayout.PropertyField(_selectCapability);

                if (_selectCapability.boolValue)
                {
                    List<string> capabilities = new List<string>(fixture.capabilityNames);
                    int numCapabilities = capabilities.Count;

                    int selectorIndex = 0;
                    string capabilityName = _capabilityName.stringValue;
                    if (!string.IsNullOrEmpty(capabilityName))
                    {
                        selectorIndex = capabilities.IndexOf(capabilityName);
                        if (selectorIndex == -1)
                        {
                            capabilities.Add(_capabilityName.stringValue + " (not available)");
                            selectorIndex = numCapabilities;
                        }
                    }

                    selectorIndex = EditorGUILayout.Popup("Capability", selectorIndex, capabilities.ToArray());
                    if (selectorIndex < numCapabilities)
                        _capabilityName.stringValue = capabilities[selectorIndex];

                    _channel.intValue = fixture.GetCapabilityChannelIndex(_capabilityName.stringValue);
                }
                else
                {
                    JSONObject fixtureDef = FixtureLibrary.Instance.GetFixtureDef(fixture.libraryPath);
                    if (fixtureDef == null)
                        return;

                    JSONArray modes = fixtureDef["modes"] as JSONArray;
                    JSONArray modeChannels = modes[fixture.modeIndex]["channels"] as JSONArray;

                    List<string> channelNames = new List<string>();
                    List<int> channelIndexes = new List<int>();

                    for (int i = 0; i < modeChannels.Count; i++)
                    {
                        string channelName = modeChannels[i];
                        if (channelName == null) continue;

                        channelNames.Add(channelName.Replace("/", "-"));
                        channelIndexes.Add(i);
                    }

                    int selectorIndex = Mathf.Max(System.Array.IndexOf(channelIndexes.ToArray(), _channel.intValue), 0);
                    selectorIndex = EditorGUILayout.Popup("Channel", selectorIndex, channelNames.ToArray());
                    _channel.intValue = channelIndexes[selectorIndex];
                }
            }
            else
                EditorGUILayout.PropertyField(_channel);

            serializedObject.ApplyModifiedProperties();
        }
    }
}