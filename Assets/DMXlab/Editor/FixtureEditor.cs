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
                fixture.numChannels = Mathf.Min(modeChannels.Count, Fixture.kMaxNumChannels);

                fixture.useChannelDefaults = EditorGUILayout.Toggle("Use Channel Defaults", fixture.useChannelDefaults);

                // channel fields

                for (int i = 0; i < fixture.numChannels; i++)
                {
                    JSONObject channel = fixture.GetChannelDef(i);
                    if (channel == null) continue;

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    if (channel["capability"] != null)
                    {
                        int value = fixture.GetChannelValue(i);
                        value = EditorGUILayout.IntSlider(channel["name"], value, 0, 255);
                        fixture.SetChannelValue(i, (byte)value);
                    }
                    else if (channel["capabilities"] != null)
                    {
                        // TODO: selector
                        int value = fixture.GetChannelValue(i);
                        value = EditorGUILayout.IntSlider(channel["name"], value, 0, 255);
                        fixture.SetChannelValue(i, (byte)value);
                    }

                    EditorGUILayout.EndVertical();
                }
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