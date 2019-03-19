using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

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

        public JSONObject fixtureDef {
            get {
                JSONObject fixtureDef = FixtureLibrary.Instance.GetFixtureDef(libraryPath);
                if (fixtureDef == null)
                    return null;

                return fixtureDef;
            }
        }

        #region needs optimize

        public string GetChannelKey(int channelIndex)
        {
            JSONArray modeChannels = fixtureDef["modes"][modeIndex]["channels"] as JSONArray;

            int n = 0;
            foreach (JSONNode channelRef in modeChannels)
            {
                if (channelRef is JSONObject)
                {
                    foreach (JSONString pixelKey in channelRef["repeatFor"] as JSONArray)
                        foreach (JSONString templateChannelName in channelRef["templateChannels"] as JSONArray)
                            if (n++ == channelIndex)
                                return templateChannelName;
                }
                else if (n++ == channelIndex)
                    return channelRef;
            }

            return "";
        }

        public string GetChannelPixelKey(int channelIndex)
        {
            JSONArray modeChannels = fixtureDef["modes"][modeIndex]["channels"] as JSONArray;

            int n = 0;
            foreach (JSONNode channelRef in modeChannels)
            {
                if (channelRef is JSONObject)
                {
                    foreach (JSONString pixelKey in channelRef["repeatFor"] as JSONArray)
                        foreach (JSONString templateChannelName in channelRef["templateChannels"] as JSONArray)
                            if (n++ == channelIndex)
                                return pixelKey;
                }
                else
                    ++n;
            }

            return "";
        }

        public int GetChannelIndex(string channelKey, string channelPixelKey)
        {
            JSONArray modeChannels = fixtureDef["modes"][modeIndex]["channels"] as JSONArray;

            int n = 0;
            foreach (JSONNode channelRef in modeChannels)
            {
                if (channelRef is JSONObject)
                {
                    foreach (JSONString pixelKey in channelRef["repeatFor"] as JSONArray)
                    {
                        foreach (JSONString templateChannelName in channelRef["templateChannels"] as JSONArray)
                        {
                            if (templateChannelName == channelKey && pixelKey == channelPixelKey)
                                return n;

                            ++n;
                        }
                    }
                }
                else 
                {
                    if (channelRef == channelKey)
                        return n;

                    ++n;
                }
            }

            return -1;
        }

        #endregion

        public JSONObject GetChannelDef(string channelKey, string channelPixelKey = "")
        {
            if (!string.IsNullOrEmpty(channelPixelKey))
                return fixtureDef["templateChannels"][channelKey] as JSONObject;

            return fixtureDef["availableChannels"][channelKey] as JSONObject;
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

        public List<string> pixelKeys;
        public Dictionary<string, List<string>> pixelGroups;

        public bool isMatrix;

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
            renderer.sharedMaterial = new Material(Shader.Find("Unlit/Color"));

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

            isMatrix = false;
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }

            pixelKeys = new List<string>();
            if (fixtureDef["matrix"] != null)
            {
                JSONArray pixelKeysZ = fixtureDef["matrix"]["pixelKeys"] as JSONArray;

                if (pixelKeysZ != null)
                {
                    for (int z = 0; z < pixelKeysZ.Count; z++)
                    {
                        JSONArray pixelKeysY = pixelKeysZ[z] as JSONArray;

                        for (int y = 0; y < pixelKeysY.Count; y++)
                        {
                            JSONArray pixelKeysX = pixelKeysY[y] as JSONArray;

                            for (int x = 0; x < pixelKeysX.Count; x++)
                            {
                                JSONString pixelKey = pixelKeysX[x] as JSONString;

                                pixelKeys.Add(pixelKey);
                                GameObject pixel = CreatePixel(pixelKey, pixelSize);

                                // TODO: pixel spacing
                                pixel.transform.localPosition = new Vector3(((pixelKeysX.Count - 1) / 2f - x) * pixelSize.x, ((pixelKeysY.Count - 1) / 2f - y) * pixelSize.y, ((pixelKeysZ.Count - 1) / 2f - z) * pixelSize.z);
                                pixel.transform.SetParent(transform, false);
                            }
                        }
                    }
                }
                else
                {
                    JSONArray pixelCount = fixtureDef["matrix"]["pixelCount"] as JSONArray;

                    for (int z = 0; z < pixelCount[2]; z++)
                    {
                        for (int y = 0; y < pixelCount[1]; y++)
                        {
                            for (int x = 0; x < pixelCount[0]; x++)
                            {
                                string pixelKey = null;
                                if (pixelCount[1] > 1)
                                {
                                    if (pixelCount[2] > 1)
                                        pixelKey = string.Format("({0}, {1}, {2})", x + 1, y + 1, z + 1);
                                    else
                                        pixelKey = string.Format("({0}, {1})", x + 1, y + 1);
                                }
                                else
                                    pixelKey = string.Format("{0}", x + 1);

                                pixelKeys.Add(pixelKey);
                                GameObject pixel = CreatePixel(pixelKey, pixelSize);

                                // TODO: pixel spacing
                                pixel.transform.localPosition = new Vector3(((pixelCount[0] - 1) / 2f - x) * pixelSize.x, ((pixelCount[1] - 1) / 2f - y) * pixelSize.y, ((pixelCount[2] - 1) / 2f - z) * pixelSize.z);
                                pixel.transform.SetParent(transform, false);
                            }
                        }
                    }
                }

                pixelGroups = new Dictionary<string, List<string>>();
                if (fixtureDef["matrix"]["pixelGroups"] != null)
                {
                    foreach (KeyValuePair<string, JSONNode> pixelGroup in fixtureDef["matrix"]["pixelGroups"] as JSONObject)
                    {
                        List<string> groupMembers = new List<string>();
                        foreach (JSONString pixelKey in pixelGroup.Value as JSONArray)
                            groupMembers.Add(pixelKey);

                        pixelGroups[pixelGroup.Key] = groupMembers;
                    }
                }
            }

            capabilityNames = new List<string>();
            capabilityChannels = new List<int>();
            for (int i = 0; i < numChannels; i++)
            {
                string channelKey = GetChannelKey(i);
                string channelPixelKey = GetChannelPixelKey(i);
                if (!string.IsNullOrEmpty(channelPixelKey))
                    isMatrix = true;

                JSONObject channel = GetChannelDef(channelKey, channelPixelKey);
                if (channel == null)
                    continue;

                JSONObject capability = channel["capability"] as JSONObject;
                if (capability != null)
                {
                    string capabilityName = capability["type"];
                    if (capability["type"] == "ColorIntensity")
                        capabilityName += " " + capability["color"];

                    capabilityNames.Add(capabilityName);
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
                string channelKey = GetChannelKey(i);
                JSONObject channel = GetChannelDef(channelKey);
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

            string channelKey = GetChannelKey(channelIndex);
            string channelPixelKey = GetChannelPixelKey(channelIndex);
            JSONObject channel = GetChannelDef(channelKey, channelPixelKey);
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
                string colorKey = capability["color"];
                float intensity = FixtureLibrary.GetFloatProperty(capability, "brightness", FixtureLibrary.Entity.Brightness, _values[channelIndex]);

                // TODO: generalize to all capabilities
                if (!string.IsNullOrEmpty(channelPixelKey))
                {
                    Transform pixel = transform.Find(channelPixelKey);
                    if (pixel == null)
                    {
                        foreach (JSONString p in fixtureDef["matrix"]["pixelGroups"][channelPixelKey] as JSONArray)
                        {
                            pixel = transform.Find(p);
                            SetPixelColor(pixel, colorKey, intensity);
                        }
                    }
                    else
                        SetPixelColor(pixel, colorKey, intensity);

                }
                else 
                {
                    if (colorKey == "Red")
                        _red = intensity;
                    else if (colorKey == "Green")
                        _green = intensity;
                    else if (colorKey == "Blue")
                        _blue = intensity;
                    else if (colorKey == "White")
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
                string wheelName = channelKey;
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
                string wheelName = channelKey;
                if (capability["wheel"] != null) wheelName = capability["wheel"];

                Wheel wheel = GetWheel(wheelName);
                if (wheel != null)
                    wheel.speed = FixtureLibrary.GetFloatProperty(capability, "speed", FixtureLibrary.Entity.Speed, _values[channelIndex]);
            }
            else if (capabilityType == "NoFunction")
            {
                // TODO this is a hack for some bad ficture defs
                if (channelKey == "Strobe")
                    _shutterEffect = FixtureLibrary.ShutterEffect.Open;

                UpdateShutter();
            }
        }

        int Mod(int k, int m)
        {
            return ((k % m) + m) % m;
        }

        void SetPixelColor(Transform pixel, string colorKey, float intensity)
        {
            MeshRenderer pixelRenderer = pixel.GetComponent<MeshRenderer>();
            Color pixelColor = pixelRenderer.sharedMaterial.color;

            if (colorKey == "Red")
                pixelColor.r = intensity;
            else if (colorKey == "Green")
                pixelColor.g = intensity;
            else if (colorKey == "Blue")
                pixelColor.b = intensity;
            else if (colorKey == "White")
                pixelColor.a = intensity;

            pixelRenderer.sharedMaterial.color = pixelColor;
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
            //Light fixtureLight = GetComponent<Light>();
            //fixtureLight.intensity = _shutter ? 0 : _intensity;
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
                if (isMatrix)
                {
                    Color color = Color.black;
                    MeshRenderer[] pixels = GetComponentsInChildren<MeshRenderer>();
                    foreach (MeshRenderer pixel in pixels)
                    {
                        color += pixel.sharedMaterial.color;
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