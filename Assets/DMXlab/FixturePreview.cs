using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;

namespace DMXlab
{

#if (UNITY_EDITOR)

    public partial class Fixture : MonoBehaviour
    {
        public bool useLibrary = true;
        public string category;

        [SerializeField]
        string _libraryPath;
        public string libraryPath {
            get { return _libraryPath; }
            set {
                if (value != _libraryPath)
                {
                    _libraryPath = value;
                    InitFixturePreview();
                }
            }
        }

        [SerializeField]
        int _modeIndex;
        public int modeIndex {
            get { return _modeIndex; }
            set {
                if (value != _modeIndex)
                {
                    _modeIndex = value;
                    InitFixturePreview();
                }
            }
        }

        [SerializeField]
        bool _useChannelDefaults = true;
        public bool useChannelDefaults {
            get { return _useChannelDefaults; }
            set {
                if (value != _useChannelDefaults)
                {
                    _useChannelDefaults = value;

                    if (_useChannelDefaults)
                        SetChannelDefaults();
                }
            }
        }

        public List<string> capabilityNames;
        public List<int> capabilityChannels;

        public int GetCapabilityChannelIndex(string capabilityName)
        {
            int index = capabilityNames.IndexOf(capabilityName);
            if (index != -1)
                return capabilityChannels[index];

            return -1;
        }

        [System.Serializable]
        public class Wheel
        {
            public float slot = 1;
            public float speed;
        }

        public List<string> wheelNames;
        public List<Wheel> wheelData;

        public Wheel GetWheel(string wheelName)
        {
            int index = wheelNames.IndexOf(wheelName);
            if (index != -1)
                return wheelData[index];

            return null;
        }

        public JSONObject GetChannelDef(int channelIndex)
        {
            JSONObject fixtureDef = FixtureLibrary.Instance.GetFixtureDef(libraryPath);
            if (fixtureDef == null)
                return null;

            string channelName = fixtureDef["modes"][modeIndex]["channels"][channelIndex];
            if (channelName != null)
            {
                JSONObject channel = fixtureDef["availableChannels"][channelName] as JSONObject;
                if (channel != null)
                    channel["name"] = channelName;

                return channel;
            }

            return null;
        }

        #region Internal

        float _pan;
        float _panTarget;
        float _panSpeed;
        float _tilt;
        float _tiltTarget;
        float _tiltSpeed;

        bool _shutter;
        FixtureLibrary.ShutterEffect _shutterEffect;
        float _shutterSpeed;

        float _intensity;
        float _red;
        float _green;
        float _blue;
        float _white;
        float _colorTemperature;

        [SerializeField] float _beamAngle;
        [SerializeField] bool _isLaser;
        [SerializeField] bool _isMatrix;

        GameObject CreatePixel(string name, Vector3 size)
        {
            GameObject go = new GameObject(name);

            MeshFilter filter = go.AddComponent<MeshFilter>();
            Mesh mesh = new Mesh();
            Vector3[] verts = new Vector3[4];
            Vector3 halfSize = size / 2;
            verts[0] = new Vector3(-halfSize.x, -halfSize.y);
            verts[1] = new Vector3(halfSize.x, -halfSize.y);
            verts[2] = new Vector3(-halfSize.x, halfSize.y);
            verts[3] = new Vector3(halfSize.x, halfSize.y);
            mesh.vertices = verts;
            mesh.triangles = new int[] { 0, 1, 2, 2, 1, 3 };
            filter.mesh = mesh;

            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.material = new Material(Shader.Find("Unlit/Color"));

            return go;
        }

        void InitFixturePreview()
        {
            JSONObject fixtureDef = FixtureLibrary.Instance.GetFixtureDef(libraryPath);
            if (fixtureDef == null)
                return;

            _isLaser = false;
            foreach (JSONString c in fixtureDef["categories"] as JSONArray)
                if (c == "Laser")
                    _isLaser = true;

            // physical

            JSONArray lens = fixtureDef["physical"]["lens"]["degreesMinMax"] as JSONArray;
            if (lens != null && lens[0] == lens[1])
                _beamAngle = lens[0];
            else
                _beamAngle = 10;

            Vector3 pixelSize = new Vector3(0.05f, 0.05f, 0);
            JSONArray dimensions = fixtureDef["physical"]["matrixPixels"]["dimensions"] as JSONArray;
            if (dimensions != null)
                pixelSize = new Vector3(dimensions[0], dimensions[1], dimensions[2]) / 1000;

            Vector3 pixelSpacing = Vector3.zero;
            JSONArray spacing = fixtureDef["physical"]["matrixPixels"]["spacing"] as JSONArray;
            if (spacing != null)
                pixelSpacing = new Vector3(spacing[0], spacing[1], spacing[2]) / 1000;

            // wheels

            wheelNames = new List<string>();
            wheelData = new List<Wheel>();
            if (fixtureDef["wheels"] != null)
            {
                foreach (KeyValuePair<string, JSONNode> wheel in fixtureDef["wheels"] as JSONObject)
                {
                    wheelNames.Add(wheel.Key);
                    wheelData.Add(new Wheel());
                }
            }

            // pixels

            _isMatrix = false;
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }

            if (fixtureDef["matrix"] != null)
            {
                _isMatrix = true;

                JSONArray pixelCount = fixtureDef["matrix"]["pixelCount"] as JSONArray;

                LightShafts lightShafts = GetComponent<LightShafts>();
                lightShafts.m_Size = new Vector3(pixelCount[0] * pixelSize.x, pixelCount[1] * pixelSize.y, pixelCount[2] * pixelSize.z);

                if (pixelCount != null)
                {
                    Vector3 offset = new Vector3((pixelCount[0] - 1f) / 2 * pixelSize.x, (pixelCount[1] - 1f) / 2 * pixelSize.y, (pixelCount[2] - 1f) / 2 * pixelSize.z);
                    int i = 1;
                    for (int z = 0; z < pixelCount[2]; z++)
                    {
                        for (int y = 0; y < pixelCount[1]; y++)
                        {
                            for (int x = 0; x < pixelCount[0]; x++)
                            {
                                GameObject pixel = CreatePixel("Pixel " + i++, pixelSize);

                                // TODO: pixel spacing
                                pixel.transform.localPosition = new Vector3(x * pixelSize.x, y * pixelSize.y, z * pixelSize.z) - offset;
                                pixel.transform.SetParent(transform, false);
                            }
                        }
                    }
                }
            }

            capabilityNames = new List<string>();
            capabilityChannels = new List<int>();
            for (int i = 0; i < numChannels; i++)
            {
                JSONObject channel = GetChannelDef(i);
                if (channel == null)
                    continue;

                JSONObject capability = channel["capability"] as JSONObject;
                if (capability != null)
                {
                    capabilityNames.Add(capability["type"]);
                    capabilityChannels.Add(i);
                }
            }

            if (useChannelDefaults)
                SetChannelDefaults();
        }

        void SetChannelDefaults()
        {
            for (int i = 0; i < numChannels; i++)
            {
                JSONObject channel = GetChannelDef(i);
                if (channel == null)
                    continue;

                int defaultValue = FixtureLibrary.ParseChannelDefault(channel);
                SetChannelValue(i, (byte)defaultValue);
            }
        }

        void UpdateChannel(int channelIndex)
        {
            if (!useLibrary)
                return;

            JSONObject channel = GetChannelDef(channelIndex);
            if (channel == null)
                return;

            JSONObject capability = channel["capability"] as JSONObject;
            if (capability == null)
            {
                JSONArray capabilities = channel["capabilities"] as JSONArray;
                for (int i = 0; i < capabilities.Count; i++)
                {
                    capability = capabilities[i] as JSONObject;
                    if (_values[channelIndex] <= capability["dmxRange"][1])
                        break;
                }
            }

            if (capability == null)
                return;

            string capabilityType = capability["type"];

            if (capabilityType == "Pan")
            {
                _panTarget = FixtureLibrary.GetFloatProperty(capability, "angle", FixtureLibrary.Entity.RotationAngle, _values[channelIndex]);

                UpdateTransform();
            }
            else if (capabilityType == "Tilt")
            {
                _tiltTarget = FixtureLibrary.GetFloatProperty(capability, "angle", FixtureLibrary.Entity.RotationAngle, _values[channelIndex]);

                UpdateTransform();
            }
            else if (capabilityType == "PanTiltSpeed")
            {
                _panSpeed = _tiltSpeed = FixtureLibrary.GetFloatProperty(capability, "speed", FixtureLibrary.Entity.Speed, _values[channelIndex]);
            }
            else if (capabilityType == "Intensity")
            {
                _intensity = FixtureLibrary.GetFloatProperty(capability, "brightness", FixtureLibrary.Entity.Brightness, _values[channelIndex]);

                UpdateIntensity();
            }
            else if (capabilityType == "ColorIntensity")
            {
                float intensity = FixtureLibrary.GetFloatProperty(capability, "brightness", FixtureLibrary.Entity.Brightness, _values[channelIndex]);

                string color = capability["color"];

                string pixelKey = FixtureLibrary.ParseTemplatePixelKey(channel["name"]);

                if (!string.IsNullOrEmpty(pixelKey))
                {
                    Transform pixel = transform.Find("Pixel " + pixelKey);
                    if (pixel == null)
                        return;

                    MeshRenderer pixelRenderer = pixel.GetComponent<MeshRenderer>();
                    Color pixelColor = pixelRenderer.sharedMaterial.color;

                    if (color == "Red")
                        pixelColor.r = intensity;
                    else if (color == "Green")
                        pixelColor.g = intensity;
                    else if (color == "Blue")
                        pixelColor.b = intensity;
                    else if (color == "White")
                        pixelColor.a = intensity;

                    pixelRenderer.sharedMaterial.color = pixelColor;
                }
                else 
                {
                    if (color == "Red")
                        _red = intensity;
                    else if (color == "Green")
                        _green = intensity;
                    else if (color == "Blue")
                        _blue = intensity;
                    else if (color == "White")
                        _white = intensity;
                }

                // TODO: more colors

                UpdateColor();
            }
            else if (capabilityType == "Zoom")
            {
                _beamAngle = FixtureLibrary.GetFloatProperty(capability, "angle", FixtureLibrary.Entity.BeamAngle, _values[channelIndex]);

                UpdateSpotAngle();
            }
            else if (capabilityType == "ShutterStrobe")
            {
                _shutterEffect = FixtureLibrary.ParseEffect(capability["shutterEffect"]);
                _shutterSpeed = FixtureLibrary.GetFloatProperty(capability, "speed", FixtureLibrary.Entity.Speed, _values[channelIndex]);

                UpdateShutter();
            }
            else if (capabilityType == "ColorTemperature")
            {
                _colorTemperature = FixtureLibrary.GetFloatProperty(capability, "colorTemperature", FixtureLibrary.Entity.ColorTemperature, _values[channelIndex]);

                UpdateColor();
            }
            else if (capabilityType == "ColorPreset")
            {
                if (capability["colors"] != null)
                {
                    Color color = FixtureLibrary.ParseColorArray(capability["colors"] as JSONArray);

                    _red = color.r;
                    _green = color.g;
                    _blue = color.b;
                }

                if (capability["colorsTemperature"] != null)
                    _colorTemperature = FixtureLibrary.GetFloatProperty(capability, "colorTemperature", FixtureLibrary.Entity.ColorTemperature, _values[channelIndex]);

                UpdateColor();
            }
            else if (capabilityType == "WheelSlot")
            {
                string wheelName = channel["name"];
                if (capability["wheel"] != null) wheelName = capability["wheel"];

                Wheel wheel = GetWheel(wheelName);
                if (wheel != null)
                {
                    wheel.slot = FixtureLibrary.GetFloatProperty(capability, "slotNumber", FixtureLibrary.Entity.SlotNumber, _values[channelIndex]);
                    wheel.speed = 0;
                }

                UpdateColor();
            }
            else if (capabilityType == "WheelRotation")
            {
                string wheelName = channel["name"];
                if (capability["wheel"] != null) wheelName = capability["wheel"];

                Wheel wheel = GetWheel(wheelName);
                if (wheel != null)
                    wheel.speed = FixtureLibrary.GetFloatProperty(capability, "speed", FixtureLibrary.Entity.Speed, _values[channelIndex]);
            }
            else if (capabilityType == "NoFunction")
            {
                // fix for some bad ficture defs
                if (channel["name"] == "Strobe")
                    _shutterEffect = FixtureLibrary.ShutterEffect.Open;

                UpdateShutter();
            }
        }

        int Mod(int k, int m)
        {
            return ((k % m) + m) % m;
        }

        void UpdateTransform()
        {
            if (Application.isPlaying)
            {
                float pan = _pan + Mathf.Sign(_panTarget - _pan) * _panSpeed * Time.deltaTime;
                _pan = (Mathf.Abs(pan - _panTarget) > Mathf.Abs(_pan - _panTarget)) ? _panTarget : pan;

                float tilt = _tilt + Mathf.Sign(_tiltTarget - _tilt) * _tiltSpeed * Time.deltaTime;
                _tilt = (Mathf.Abs(tilt - _tiltTarget) > Mathf.Abs(_tilt - _tiltTarget)) ? _tiltTarget : tilt;
            }
            else
            {
                _pan = _panTarget;
                _tilt = _tiltTarget;
            }

            transform.localRotation = Quaternion.AngleAxis(_pan, Vector3.down) * Quaternion.AngleAxis(_tilt, Vector3.left);
        }

        void UpdateShutter()
        {
            if (Application.isPlaying && _shutterEffect > FixtureLibrary.ShutterEffect.Closed)
            {
                // TODO: animation curves

                int strobePeriod = (int)(60 / _shutterSpeed) + 1;
                if (Time.frameCount % strobePeriod == 0)
                    _shutter = !_shutter;
            }
            else
                _shutter = (_shutterEffect == FixtureLibrary.ShutterEffect.Closed);

            UpdateIntensity();
        }

        void UpdateIntensity()
        {
            Light fixtureLight = GetComponent<Light>();
            fixtureLight.intensity = _shutter ? 0 : _intensity;
        }

        void UpdateSpotAngle()
        {
            Light fixtureLight = GetComponent<Light>();
            fixtureLight.spotAngle = _isLaser ? 0 : _beamAngle;
        }

        void UpdateColor()
        {
            JSONObject fixtureDef = FixtureLibrary.Instance.GetFixtureDef(libraryPath);
            if (fixtureDef == null)
                return;

            Light fixtureLight = GetComponent<Light>();

            for (int i = 0; i < wheelNames.Count; i++)
            {
                string wheelName = wheelNames[i];
                Wheel wheel = wheelData[i];

                JSONArray slots = fixtureDef["wheels"][wheelName]["slots"] as JSONArray;
                int numSlots = slots.Count;
                int slotIndex = (int)wheel.slot;
                float t = wheel.slot - slotIndex;
                JSONObject slotA = slots[Mod(slotIndex - 1, numSlots)] as JSONObject;
                JSONObject slotB = slots[Mod(slotIndex, numSlots)] as JSONObject;

                if (slotA["type"] == "Color")
                {
                    Color colorA = FixtureLibrary.ParseColorArray(slotA["colors"] as JSONArray);
                    Color colorB = FixtureLibrary.ParseColorArray(slotB["colors"] as JSONArray);
                    Color color = (1 - t) * colorA + t * colorB;

                    _red = color.r;
                    _green = color.g;
                    _blue = color.b;

                    // TODO: color temperature
                }

                // wheel rotation
                if (Application.isPlaying)
                {
                    wheel.slot += wheel.speed * Time.deltaTime;
                    if (wheel.slot >= numSlots + 1) wheel.slot -= numSlots;
                    if (wheel.slot < 1) wheel.slot += numSlots;
                }
            }

            // color temperature mode
            if (_colorTemperature > 0)
            {
                fixtureLight.color = Color.white;
                fixtureLight.colorTemperature = _colorTemperature;
            }
            else
            {
                if (_isMatrix)
                {
                    Color color = Color.black;
                    MeshRenderer[] pixels = GetComponentsInChildren<MeshRenderer>();
                    foreach (MeshRenderer pixel in pixels)
                    {
                        color += pixel.material.color;
                    }
                    fixtureLight.color = color / pixels.Length;
                }
                else
                    fixtureLight.color = new Color(_red + _white, _green + _white, _blue + _white);

                fixtureLight.colorTemperature = 6500;
            }
        }

        void UpdatePreview()
        {
            // TODO: user-defined channel semantics
            if (!useLibrary)
                return;

            UpdateTransform();
            UpdateShutter();
            UpdateSpotAngle();
            UpdateColor();
        }

        #endregion

        void Awake()
        {
            for (int i = 0; i < numChannels; i++)
                UpdateChannel(i);
        }
    }

#else
        
    public partial class Fixture : MonoBehaviour
    {
        void UpdateChannel(int channelIndex) {}

        void UpdatePreview() {}

        void Start()
        {
            Light light = gameObject.GetComponent<Light>();
            if (light) light.enabled = false;

            LightShafts lightShafts = gameObject.GetComponent<LightShafts>();
            if (lightShafts) lightShafts.enabled = false;

            // FIXME: disable pixels
        }
    }

#endif

}