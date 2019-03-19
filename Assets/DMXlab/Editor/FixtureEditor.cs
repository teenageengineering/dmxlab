using UnityEngine;
using UnityEditor;
using SimpleJSON;

namespace DMXlab
{
    [CustomEditor(typeof(Fixture))]
    public class FixtureEditor : Editor
    {
        #region Menu

        [MenuItem("GameObject/DMXlab/Fixture", false, 10)]
        static void CreateFixture()
        {
            GameObject go = new GameObject("Fixture");
            go.AddComponent<Fixture>();

            Light light = go.GetComponent<Light>();
            if (!light) light = go.AddComponent<Light>();
            light.type = LightType.Spot;
            light.shadows = LightShadows.Hard;

            LightShafts lightShafts = go.GetComponent<LightShafts>();
            if (!lightShafts) lightShafts = go.AddComponent<LightShafts>();
            lightShafts.m_CurrentCamera = Camera.main;
            lightShafts.m_DepthThreshold = 4;

            Selection.activeGameObject = go;
        }

        [MenuItem("GameObject/DMXlab/Smoke", false, 10)]
        static void CreateSmoke()
        {
            GameObject go = new GameObject("Smoke");
            go.AddComponent<Smoke>();

            Selection.activeGameObject = go;
        }

        #endregion

        public override bool RequiresConstantRepaint()
        {
            return Application.isPlaying;
        }

        public override void OnInspectorGUI()
        {
            Fixture fixture = target as Fixture;

            EditorStyles.label.wordWrap = true;

            fixture.dmxDriver = (Driver)EditorGUILayout.ObjectField("Driver", fixture.dmxDriver, typeof(Driver), true);

            fixture.startAdress = Mathf.Clamp(EditorGUILayout.IntField("Start Adress", fixture.startAdress), 1, 512);

            fixture.useLibrary = EditorGUILayout.Toggle("Use Fixture Library", fixture.useLibrary);

            if (fixture.useLibrary)
            {
                string[] categories = FixtureLibrary.Instance.GetCategories();
                int categoryIndex = Mathf.Max(System.Array.IndexOf(categories, fixture.category), 0);
                categoryIndex = EditorGUILayout.Popup("Category", categoryIndex, categories);
                fixture.category = categories[categoryIndex];

                string[] fixtures = FixtureLibrary.Instance.FixturPathsForCategory(fixture.category);
                int fixtureIndex = Mathf.Max(System.Array.IndexOf(fixtures, fixture.libraryPath), 0);
                fixtureIndex = EditorGUILayout.Popup("Fixture", fixtureIndex, fixtures);
                fixture.libraryPath = fixtures[fixtureIndex];

                JSONObject fixtureDef = FixtureLibrary.Instance.GetFixtureDef(fixture.libraryPath);
                if (fixtureDef == null)
                    return;

                JSONArray modes = fixtureDef["modes"] as JSONArray;
                string[] modeNames = new string[modes.Count];
                for (int i = 0; i < modes.Count; i++) modeNames[i] = modes[i]["name"];
                fixture.modeIndex = Mathf.Min(EditorGUILayout.Popup("Mode", fixture.modeIndex, modeNames), modes.Count - 1);

                JSONArray modeChannels = modes[fixture.modeIndex]["channels"] as JSONArray;

                fixture.useChannelDefaults = EditorGUILayout.Toggle("Use Channel Defaults", fixture.useChannelDefaults);

                // channel fields

                int n = 0;
                foreach (JSONNode channelRef in modeChannels)
                {
                    // matrix insert block
                    if (channelRef is JSONObject)
                    {
                        foreach (JSONString pixelKey in channelRef["repeatFor"] as JSONArray)
                        {
                            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                            foreach (JSONString templateChannelName in channelRef["templateChannels"] as JSONArray)
                            {
                                string channelName = FixtureLibrary.ExpandTemplateChannelName(templateChannelName, pixelKey);
                                JSONObject templateChannel = fixture.GetChannelDef(templateChannelName, pixelKey);

                                // TODO: duplicated code
                                if (templateChannel["capability"] != null)
                                {
                                    int value = fixture.GetChannelValue(n);
                                    value = EditorGUILayout.IntSlider(channelName, value, 0, 255);
                                    fixture.SetChannelValue(n, (byte)value);
                                }
                                else if (templateChannel["capabilities"] != null)
                                {
                                    // TODO: selector
                                    int value = fixture.GetChannelValue(n);
                                    value = EditorGUILayout.IntSlider(channelName, value, 0, 255);
                                    fixture.SetChannelValue(n, (byte)value);
                                }

                                ++n;
                            }

                            EditorGUILayout.EndVertical();
                        }
                    }
                    else if (false)
                    {
                        // TODO: explicit pixel keys / groups 
                    }
                    else
                    {
                        JSONObject channel = fixture.GetChannelDef(channelRef);

                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                        if (channel["capability"] != null)
                        {
                            int value = fixture.GetChannelValue(n);
                            value = EditorGUILayout.IntSlider(channelRef, value, 0, 255);
                            fixture.SetChannelValue(n, (byte)value);
                        }
                        else if (channel["capabilities"] != null)
                        {
                            // TODO: selector
                            int value = fixture.GetChannelValue(n);
                            value = EditorGUILayout.IntSlider(channelRef, value, 0, 255);
                            fixture.SetChannelValue(n, (byte)value);
                        }

                        ++n;

                        EditorGUILayout.EndVertical();
                    }
                }

                fixture.numChannels = Mathf.Min(n, Fixture.kMaxNumChannels);
            }
            else
            {
                fixture.numChannels = EditorGUILayout.IntSlider("Num Channels", fixture.numChannels, 1, Fixture.kMaxNumChannels);

                for (int i = 0; i < fixture.numChannels; i++)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    int value = fixture.GetChannelValue(i);
                    value = EditorGUILayout.IntSlider("Channel " + (i + 1), value, 0, 255);
                    fixture.SetChannelValue(i, (byte)value);

                    EditorGUILayout.EndVertical();
                }
            }
        }
    }
}