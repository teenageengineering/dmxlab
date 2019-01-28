using UnityEngine;
using System.Collections;
using UnityEditor;

namespace DP
{
    [ExecuteInEditMode]
    [CustomEditor(typeof(DMX))]
    public class DMXInspector : Editor
    {
        #region Menu

        [MenuItem("GameObject/DMXlab/DMX Sender", false, 10)]
        static void CreateDMX()
        {
            GameObject go = new GameObject("DMX Sender");
            go.AddComponent<DMX>();
            Selection.activeGameObject = go;
        }

        #endregion

        public override void OnInspectorGUI()
        {
            var script = (DMX)target;

            // TODO: copy missing device handing from MidiEndpointEditor

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Serial ports: ");
            string[] serialPorts = script.serialPorts.ToArray();
            int i = EditorGUILayout.Popup(script.serialPortIdx, serialPorts);
            if (i < serialPorts.Length && i != script.serialPortIdx)
            {
                script.serialPortIdx = i;
                script.OpenSerialPort();
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}