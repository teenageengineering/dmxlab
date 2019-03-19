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
        SerializedProperty _templateChannel;
        SerializedProperty _pixelKey;
        SerializedProperty _selectCapability;
        SerializedProperty _capabilityName;

        void OnEnable()
        {
            _fixture = serializedObject.FindProperty("_fixture");
            _channel = serializedObject.FindProperty("_channel");
            _templateChannel = serializedObject.FindProperty("_templateChannel");
            _pixelKey = serializedObject.FindProperty("_pixelKey");
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
                    if (fixture.isMatrix)
                    {
                        List<string> templateChannelNames = new List<string>();
                        List<string> pixelKeys = new List<string>();

                        for (int i = 0; i < fixture.numChannels; i++)
                        {
                            string channelKey = fixture.GetChannelKey(i);
                            string channelPixelKey = fixture.GetChannelPixelKey(i);
                            if (string.IsNullOrEmpty(channelPixelKey))
                                continue;

                            if (!pixelKeys.Contains(channelPixelKey))
                                pixelKeys.Add(channelPixelKey);

                            if (!templateChannelNames.Contains(channelKey))
                                templateChannelNames.Add(channelKey);
                        }

                        int pixelIndex = Mathf.Max(System.Array.IndexOf(pixelKeys.ToArray(), _pixelKey.stringValue), 0);
                        pixelIndex = EditorGUILayout.Popup("Pixel Key", pixelIndex, pixelKeys.ToArray());
                        _pixelKey.stringValue = pixelKeys[pixelIndex];

                        int templateChannelIndex = Mathf.Max(System.Array.IndexOf(templateChannelNames.ToArray(), _templateChannel.stringValue), 0);
                        templateChannelIndex = EditorGUILayout.Popup("Pixel Channel", templateChannelIndex, templateChannelNames.ToArray());
                        _templateChannel.stringValue = templateChannelNames[templateChannelIndex];

                        _channel.intValue = fixture.GetChannelIndex(_templateChannel.stringValue, _pixelKey.stringValue);
                    }
                    else
                    {
                        List<string> channelNames = new List<string>();
                        List<int> channelIndexes = new List<int>();

                        for (int i = 0; i < fixture.numChannels; i++)
                        {
                            string channelKey = fixture.GetChannelKey(i);
                            JSONObject channel = fixture.GetChannelDef(channelKey);
                            if (channel == null)
                                continue;

                            channelNames.Add(channelKey.Replace("/", "-"));
                            channelIndexes.Add(i);
                        }

                        int selectorIndex = Mathf.Max(System.Array.IndexOf(channelIndexes.ToArray(), _channel.intValue), 0);
                        selectorIndex = EditorGUILayout.Popup("Channel", selectorIndex, channelNames.ToArray());
                        _channel.intValue = channelIndexes[selectorIndex];
                    }
                }
            }
            else
                EditorGUILayout.PropertyField(_channel);

            serializedObject.ApplyModifiedProperties();
        }
    }
}