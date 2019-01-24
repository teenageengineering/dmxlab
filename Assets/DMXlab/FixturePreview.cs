using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;

namespace DMXlab
{

#if (UNITY_EDITOR)

    public partial class Fixture : MonoBehaviour
    {
        public bool useLibrary;
        public string category;

        string _libraryPath;
        public string libraryPath {
            get { return _libraryPath; }
            set {
                if (value != _libraryPath)
                {
                    _libraryPath = value;
                    _sceneObjectNeedsInit = true;
                }
            }
        }

        int _modeIndex;
        public int modeIndex {
            get { return _modeIndex; }
            set {
                if (value != _modeIndex)
                {
                    _modeIndex = value;
                    _sceneObjectNeedsInit = true;
                }
            }
        }

        bool _useChannelDefaults;
        public bool useChannelDefaults {
            get { return _useChannelDefaults; }
            set {
                if (value != _useChannelDefaults)
                {
                    _useChannelDefaults = value;
                    _sceneObjectNeedsInit = true;
                }
            }
        }

        public JSONObject fixtureDef {
            get { return FixtureLibrary.Instance.GetFixtureDef(libraryPath); }
        }

        Dictionary<string, int> _capabilityChannels = new Dictionary<string, int>();
        public List<string> GetCapabilities()
        {
            return new List<string>(_capabilityChannels.Keys);
        }
        public int GetCapabilityChannelIndex(string capability)
        {
            if (_capabilityChannels.ContainsKey(capability))
                return _capabilityChannels[capability];

            return -1;
        }

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
        float _beamAngle;

        bool _isLaser;

        class Wheel
        {
            public float slot = 1;
            public float speed;
        }

        Dictionary<string, Wheel> _wheels = new Dictionary<string, Wheel>();

        public JSONObject GetChannelDef(int channelIndex)
        { 
            if (!useLibrary)
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

        bool _sceneObjectNeedsInit;
        void InitSceneObjectIfNeeded()
        {
            if (!_sceneObjectNeedsInit)
                return;

            foreach (JSONString c in fixtureDef["categories"] as JSONArray)
                if (c == "Laser")
                    _isLaser = true;

            JSONArray lens = fixtureDef["physical"]["lens"]["degreesMinMax"] as JSONArray;
            if (lens != null && lens[0] == lens[1])
                _beamAngle = lens[0];
            else
                _beamAngle = 10;

            _shutterEffect = FixtureLibrary.ShutterEffect.Open;
            _intensity = 1;

            _wheels = new Dictionary<string, Wheel>();
            if (fixtureDef["wheels"] != null)
                foreach (KeyValuePair<string, JSONNode> wheel in fixtureDef["wheels"] as JSONObject)
                    _wheels.Add(wheel.Key, new Wheel());

            _capabilityChannels = new Dictionary<string, int>();
            for (int i = 0; i < numChannels; i++)
            {
                JSONObject channel = GetChannelDef(i);
                if (channel == null)
                    continue;

                if (useChannelDefaults)
                {
                    int defaultValue = FixtureLibrary.ParseChannelDefault(channel);

                    SetChannelValue(i, (byte)defaultValue);
                }

                JSONObject capability = channel["capability"] as JSONObject;
                if (capability != null)
                    _capabilityChannels[capability["type"]] = i;
            }

            _sceneObjectNeedsInit = false;
        }

        void UpdateSceneObject(int channelIndex)
        {
            InitSceneObjectIfNeeded();

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
            }
            else if (capabilityType == "Tilt")
            {
                _tiltTarget = FixtureLibrary.GetFloatProperty(capability, "angle", FixtureLibrary.Entity.RotationAngle, _values[channelIndex]);
            }
            else if (capabilityType == "PanTiltSpeed")
            {
                _panSpeed = _tiltSpeed = FixtureLibrary.GetFloatProperty(capability, "speed", FixtureLibrary.Entity.Speed, _values[channelIndex]);
            }
            else if (capabilityType == "Intensity")
            {
                _intensity = FixtureLibrary.GetFloatProperty(capability, "brightness", FixtureLibrary.Entity.Brightness, _values[channelIndex]);
            }
            else if (capabilityType == "ColorIntensity")
            {
                float intensity = FixtureLibrary.GetFloatProperty(capability, "brightness", FixtureLibrary.Entity.Brightness, _values[channelIndex]);

                string color = capability["color"];

                if (color == "Red")
                    _red = intensity;
                else if (color == "Green")
                    _green = intensity;
                else if (color == "Blue")
                    _blue = intensity;
                else if (color == "White")
                    _white = intensity;

                // TODO: more colors
            }
            else if (capabilityType == "Zoom")
            {
                _beamAngle = FixtureLibrary.GetFloatProperty(capability, "angle", FixtureLibrary.Entity.BeamAngle, _values[channelIndex]);
            }
            else if (capabilityType == "ShutterStrobe")
            {
                _shutterEffect = FixtureLibrary.ParseEffect(capability["shutterEffect"]);
                _shutterSpeed = FixtureLibrary.GetFloatProperty(capability, "speed", FixtureLibrary.Entity.Speed, _values[channelIndex]);
            }
            else if (capabilityType == "ColorTemperature")
            {
                _colorTemperature = FixtureLibrary.GetFloatProperty(capability, "colorTemperature", FixtureLibrary.Entity.ColorTemperature, _values[channelIndex]);
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
            }
            else if (capabilityType == "WheelSlot")
            {
                string wheelName = channel["name"];
                if (capability["wheel"] != null) wheelName = capability["wheel"];

                _wheels[wheelName].slot = FixtureLibrary.GetFloatProperty(capability, "slotNumber", FixtureLibrary.Entity.SlotNumber, _values[channelIndex]);
                _wheels[wheelName].speed = 0;
            }
            else if (capabilityType == "WheelRotation")
            {
                string wheelName = channel["name"];
                if (capability["wheel"] != null) wheelName = capability["wheel"];

                _wheels[wheelName].speed = FixtureLibrary.GetFloatProperty(capability, "speed", FixtureLibrary.Entity.Speed, _values[channelIndex]);
            }
        }

        Light GetLight()
        {
            Light light = gameObject.GetComponent<Light>();
            if (!light)
            {
                light = gameObject.AddComponent<Light>();
                light.type = LightType.Spot;
                light.shadows = LightShadows.Hard;
            }

            LightShafts lightShafts = gameObject.GetComponent<LightShafts>();
            if (!lightShafts)
            {
                lightShafts = gameObject.AddComponent<LightShafts>();
                lightShafts.m_CurrentCamera = Camera.main;
                lightShafts.m_EpipolarSamples = 1024;
                lightShafts.m_EpipolarLines = 512;
            }

            return light;
        }

        int Mod(int k, int m)
        {
            return ((k % m) + m) % m;
        }

        #region MonoBehaviour

        void Start()
        {
            _sceneObjectNeedsInit = true;
        }

        void Update()
        {
            InitSceneObjectIfNeeded();

            float pan = Mathf.Repeat(_pan + Mathf.Sign(_panTarget - _pan) * _panSpeed * Time.deltaTime, 360);
            _pan = (Mathf.Abs(pan - _panTarget) > Mathf.Abs(_pan - _panTarget)) ? _panTarget : pan;

            float tilt = Mathf.Repeat(_tilt + Mathf.Sign(_tiltTarget - _tilt) * _tiltSpeed * Time.deltaTime, 360);
            _tilt = (Mathf.Abs(tilt - _tiltTarget) > Mathf.Abs(_tilt - _tiltTarget)) ? _tiltTarget : tilt;

            transform.localRotation = Quaternion.AngleAxis(_pan, Vector3.down) * Quaternion.AngleAxis(_tilt, Vector3.left);

            Light light = GetLight();

            if (_shutterEffect > FixtureLibrary.ShutterEffect.Closed)
            {
                // TODO: animation curves

                int strobePeriod = (int)(60 / _shutterSpeed) + 1;
                if (Time.frameCount % strobePeriod == 0)
                    _shutter = !_shutter;
            }
            else
                _shutter = (_shutterEffect == FixtureLibrary.ShutterEffect.Closed);

            light.intensity = _shutter ? 0 : _intensity;
            light.spotAngle = _isLaser ? 0 : _beamAngle;

            // wheels

            foreach (KeyValuePair<string, Wheel> wheel in _wheels)
            {
                JSONArray slots = fixtureDef["wheels"][wheel.Key]["slots"] as JSONArray;
                int numSlots = slots.Count;
                int slotIndex = (int)wheel.Value.slot;
                float t = wheel.Value.slot - slotIndex;
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
                wheel.Value.slot += wheel.Value.speed * Time.deltaTime;
                if (wheel.Value.slot >= numSlots + 1) wheel.Value.slot -= numSlots;
                if (wheel.Value.slot < 1) wheel.Value.slot += numSlots;
            }

            // color temperature mode
            if (_colorTemperature > 0)
            {
                light.color = Color.white;
                light.colorTemperature = _colorTemperature;
            }
            else
            {
                light.color = new Color(_red + _white, _green + _white, _blue + _white);
                light.colorTemperature = 6500;
            }
        }

        public void OnValidate()
        {
            if (!Application.isPlaying)
                Update();
        }

        #endregion
    }

#else
        
    public partial class Fixture : MonoBehaviour
    {
        void UpdateSceneObject(int channelIndex) {}

        void Start()
        {
            Light light = gameObject.GetComponent<Light>();
            if (light) light.enabled = false;

            LightShafts lightShafts = gameObject.GetComponent<LightShafts>();
            if (lightShafts) lightShafts.enabled = false;
        }
    }

#endif

}