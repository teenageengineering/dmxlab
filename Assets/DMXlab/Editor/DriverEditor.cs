using UnityEngine;
using UnityEditor;
using System.IO.Ports;

namespace DMXlab
{
    [CustomEditor(typeof(Driver))]
    public class DriverEditor : Editor
    {
        #region Menu

        [MenuItem("GameObject/DMXlab/Driver", false, 10)]
        static void CreateDriver()
        {
            GameObject go = new GameObject("Driver");
            go.AddComponent<Driver>();
            Selection.activeGameObject = go;
        }

        #endregion

        public override void OnInspectorGUI()
        {
            var dmxDriver = (Driver)target;

            string[] serialPorts = Driver.GetPortNames();
            int serialPortIndex = Mathf.Max(System.Array.IndexOf(serialPorts, dmxDriver.serialPortName), 0);
            serialPortIndex = EditorGUILayout.Popup("Serial ports", serialPortIndex, serialPorts);
            dmxDriver.serialPortName = serialPorts[serialPortIndex];
        }
    }
}