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

        void InitFixturePreview()
        {
            JSONObject fixtureDef = FixtureLibrary.Instance.GetFixtureDef(libraryPath);
            if (fixtureDef == null)
                return;

            foreach (JSONString c in fixtureDef["categories"] as JSONArray)
                if (c == "Laser")
                    _isLaser = true;

            JSONArray lens = fixtureDef["physical"]["lens"]["degreesMinMax"] as JSONArray;
            if (lens != null && lens[0] == lens[1])
                _beamAngle = lens[0];
            else
                _beamAngle = 10;

            wheelNames = new List<string>();
            wheelData = new List<Wheel>();
            if (fixtureDef["wheels"] != null)
                foreach (KeyValuePair<string, JSONNode> wheel in fixtureDef["wheels"] as JSONObject)
                {
                    wheelNames.Add(wheel.Key);
                    wheelData.Add(new Wheel());
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

            // TODO: init channels
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

                if (color == "Red")
                    _red = intensity;
                else if (color == "Green")
                    _green = intensity;
                else if (color == "Blue")
                    _blue = intensity;
                else if (color == "White")
                    _white = intensity;

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

        void Start()
        {
            for (int i = 0; i < numChannels; i++)
                UpdateChannel(i);

            UpdatePreview();
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
        }
    }

#endif

}