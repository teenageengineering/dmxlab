using UnityEngine;
using UnityEditor;
using System.IO.Ports;

namespace DMXlab
{
    [CustomEditor(typeof(DMXDriver))]
    public class DMXDriverEditor : Editor
    {
        #region Menu

        [MenuItem("GameObject/DMXlab/DMX Driver", false, 10)]
        static void CreateDMXDriver()
        {
            GameObject go = new GameObject("DMX Driver");
            go.AddComponent<DMXDriver>();
            Selection.activeGameObject = go;
        }

        #endregion

        public override void OnInspectorGUI()
        {
            var dmxDriver = (DMXDriver)target;

            string[] serialPorts = DMXDriver.GetPortNames();
            int serialPortIndex = Mathf.Max(System.Array.IndexOf(serialPorts, dmxDriver.serialPortName), 0);
            serialPortIndex = EditorGUILayout.Popup("Serial ports", serialPortIndex, serialPorts);
            dmxDriver.serialPortName = serialPorts[serialPortIndex];
        }
    }
}