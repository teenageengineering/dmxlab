using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;
using System.Text.RegularExpressions;

namespace DMXlab
{
    public class FixtureLibrary {

        private static readonly FixtureLibrary _instance = new FixtureLibrary();

        const string kLibraryPath = "open-fixture-library/fixtures/";

        private FixtureLibrary()
        {
            TextAsset registerAsset = (TextAsset)Resources.Load(kLibraryPath + "register");
            _register = (JSONObject)JSON.Parse(registerAsset.text);
            _fixtureDefs = new Dictionary<string, JSONObject>();
        }

        public static FixtureLibrary Instance {
            get { 
                return _instance; 
            }
        }

        JSONObject _register;
        Dictionary<string, JSONObject> _fixtureDefs;

        public JSONObject GetFixtureDef(string path)
        {
            if (path == null)
                return null;

            if (!_fixtureDefs.ContainsKey(path))
            {
                string resourcePath = kLibraryPath + path;
                TextAsset fixtureAsset = (TextAsset)Resources.Load(resourcePath);
                _fixtureDefs[path] = (JSONObject)JSON.Parse(fixtureAsset.text);
            }

            return _fixtureDefs[path];
        }

        public string[] GetCategories()
        {
            List<string> categories = new List<string>();
            categories.Add("Any");
            foreach (KeyValuePair<string, JSONNode> pair in _register["categories"] as JSONObject)
                categories.Add(pair.Key);

            return categories.ToArray();
        }

        public string[] FixturPathsForCategory(string category)
        {
            List<string> paths = new List<string>();

            if (category == "Any")
            {
                foreach (KeyValuePair<string, JSONNode> manufacturer in _register["manufacturers"] as JSONObject)
                    foreach (JSONString fixture in manufacturer.Value as JSONArray)
                        paths.Add(manufacturer.Key + "/" + fixture);
            }
            else
            {
                foreach (JSONString path in _register["categories"][category] as JSONArray)
                    paths.Add(path);
            }

            return paths.ToArray();
        }

        #region Entities

        public delegate float PropertyParser(string stringValue);

        const float kSpeedRangeHz = 100;

        public static float ParseSpeed(string speed)
        {
            float sign = Regex.IsMatch(speed, "reverse", RegexOptions.IgnoreCase) ? -1 : 1;

            if (Regex.IsMatch(speed, "slow", RegexOptions.IgnoreCase))
                return sign / 100 * kSpeedRangeHz;
            if (Regex.IsMatch(speed, "fast", RegexOptions.IgnoreCase))
                return sign * kSpeedRangeHz;

            if (Regex.IsMatch(speed, "hz", RegexOptions.IgnoreCase))
                return System.Single.Parse(Regex.Replace(speed, "hz", "", RegexOptions.IgnoreCase));

            if (Regex.IsMatch(speed, "bpm", RegexOptions.IgnoreCase))
                return System.Single.Parse(Regex.Replace(speed, "bpm", "", RegexOptions.IgnoreCase)) / 60;

            if (Regex.IsMatch(speed, "%", RegexOptions.IgnoreCase))
                return System.Single.Parse(Regex.Replace(speed, "%", "", RegexOptions.IgnoreCase)) / 100 * kSpeedRangeHz;

            return 0;
        }

        public static float ParseRotationSpeed(string speed)
        {
            float sign = Regex.IsMatch(speed, "ccw", RegexOptions.IgnoreCase) ? -1 : 1;

            if (Regex.IsMatch(speed, "slow", RegexOptions.IgnoreCase))
                return sign / 100 * kSpeedRangeHz;
            if (Regex.IsMatch(speed, "fast", RegexOptions.IgnoreCase))
                return sign * kSpeedRangeHz;

            if (Regex.IsMatch(speed, "hz", RegexOptions.IgnoreCase))
                return System.Single.Parse(Regex.Replace(speed, "hz", "", RegexOptions.IgnoreCase));

            if (Regex.IsMatch(speed, "rpm", RegexOptions.IgnoreCase))
                return System.Single.Parse(Regex.Replace(speed, "bpm", "", RegexOptions.IgnoreCase)) / 60;

            if (Regex.IsMatch(speed, "%", RegexOptions.IgnoreCase))
                return System.Single.Parse(Regex.Replace(speed, "%", "", RegexOptions.IgnoreCase)) / 100 * kSpeedRangeHz;

            return 0;
        }

        const float kTimeRangeSec = 10;

        public static float ParseTime(string time)
        {
            if (Regex.IsMatch(time, "short", RegexOptions.IgnoreCase))
                return 1f / 100 * kTimeRangeSec;
            if (Regex.IsMatch(time, "long", RegexOptions.IgnoreCase))
                return kTimeRangeSec;

            if (Regex.IsMatch(time, "s", RegexOptions.IgnoreCase))
                return System.Single.Parse(Regex.Replace(time, "s", "", RegexOptions.IgnoreCase));

            if (Regex.IsMatch(time, "ms", RegexOptions.IgnoreCase))
                return System.Single.Parse(Regex.Replace(time, "ms", "", RegexOptions.IgnoreCase)) * 1000;

            if (Regex.IsMatch(time, "%", RegexOptions.IgnoreCase))
                return System.Single.Parse(Regex.Replace(time, "%", "", RegexOptions.IgnoreCase)) / 100 * kTimeRangeSec;

            return 0;
        }

        public static float ParseBrightness(string brightness)
        {
            if (Regex.IsMatch(brightness, "dark", RegexOptions.IgnoreCase))
                return 1f / 100;
            if (Regex.IsMatch(brightness, "bright", RegexOptions.IgnoreCase))
                return 1;

            if (Regex.IsMatch(brightness, "lm", RegexOptions.IgnoreCase))
                return System.Single.Parse(Regex.Replace(brightness, "lm", "", RegexOptions.IgnoreCase)) / 10000;

            if (Regex.IsMatch(brightness, "%", RegexOptions.IgnoreCase))
                return System.Single.Parse(Regex.Replace(brightness, "%", "", RegexOptions.IgnoreCase)) / 100;

            return 0;
        }

        const float kColorTemperatureDefaultK = 6500;
        const float kColorTemperatureRangeK = 3000;

        public static float ParseColorTemperature(string temperature)
        {
            if (Regex.IsMatch(temperature, "warm|cto", RegexOptions.IgnoreCase))
                return kColorTemperatureDefaultK - kColorTemperatureRangeK;
            if (Regex.IsMatch(temperature, "cold|ctb", RegexOptions.IgnoreCase))
                return kColorTemperatureDefaultK + kColorTemperatureRangeK;

            if (Regex.IsMatch(temperature, "K", RegexOptions.IgnoreCase))
                return System.Single.Parse(Regex.Replace(temperature, "K", "", RegexOptions.IgnoreCase));

            if (Regex.IsMatch(temperature, "%", RegexOptions.IgnoreCase))
                return kColorTemperatureDefaultK + System.Single.Parse(Regex.Replace(temperature, "%", "", RegexOptions.IgnoreCase)) / 100 * kColorTemperatureRangeK;

            return kColorTemperatureDefaultK;
        }

        const float kRotationAngleRangeDeg = 360;

        public static float ParseRotationAngle(string angle)
        {
            if (Regex.IsMatch(angle, "deg", RegexOptions.IgnoreCase))
                return System.Single.Parse(Regex.Replace(angle, "deg", "", RegexOptions.IgnoreCase));

            if (Regex.IsMatch(angle, "%", RegexOptions.IgnoreCase))
                return System.Single.Parse(Regex.Replace(angle, "%", "", RegexOptions.IgnoreCase)) / 100 * kRotationAngleRangeDeg;

            return 0;
        }

        const float kBeamAngleMinDeg = 5;
        const float kBeamAngleRangeDeg = 40;

        public static float ParseBeamAngle(string angle)
        {
            if (Regex.IsMatch(angle, "narrow", RegexOptions.IgnoreCase))
                return kBeamAngleMinDeg;

            if (Regex.IsMatch(angle, "wide", RegexOptions.IgnoreCase))
                return kBeamAngleMinDeg + kBeamAngleRangeDeg;

            if (Regex.IsMatch(angle, "deg", RegexOptions.IgnoreCase))
                return System.Single.Parse(Regex.Replace(angle, "deg", "", RegexOptions.IgnoreCase));

            if (Regex.IsMatch(angle, "%", RegexOptions.IgnoreCase))
                return kBeamAngleMinDeg + System.Single.Parse(Regex.Replace(angle, "%", "", RegexOptions.IgnoreCase)) / 100 * kBeamAngleRangeDeg;

            return 0;
        }

        public static float ParseParameter(string param)
        {
            if (Regex.IsMatch(param, "off|instant", RegexOptions.IgnoreCase))
                return 0;

            if (Regex.IsMatch(param, "low|slow|small|short", RegexOptions.IgnoreCase))
                return 1f / 100;

            if (Regex.IsMatch(param, "high|fast|big|long", RegexOptions.IgnoreCase))
                return 1;

            if (Regex.IsMatch(param, "%", RegexOptions.IgnoreCase))
                return kBeamAngleMinDeg + System.Single.Parse(Regex.Replace(param, "%", "", RegexOptions.IgnoreCase)) / 100;

            return System.Single.Parse(param);
        }

        public static float ParseSlotNumber(string number)
        {
            return System.Single.Parse(number);
        }

        public static float ParsePercent(string percent)
        {
            if (Regex.IsMatch(percent, "low", RegexOptions.IgnoreCase))
                return 1f / 100;

            if (Regex.IsMatch(percent, "high", RegexOptions.IgnoreCase))
                return 1;

            if (Regex.IsMatch(percent, "%", RegexOptions.IgnoreCase))
                return System.Single.Parse(Regex.Replace(percent, "%", "", RegexOptions.IgnoreCase)) / 100;

            return 0;
        }

        public static Color ParseColorArray(JSONArray colorArray)
        {
            Color color = Color.black;

            if (colorArray == null)
                return color;

            foreach (JSONString hexString in colorArray)
            {
                Color c; ColorUtility.TryParseHtmlString(hexString, out c);
                color += c;
            }

            return color;
        }

        public enum ShutterEffect {
            Open, Closed, Strobe, Pulse, RampUp, RampDown, RampUpDown, Lightning, Spikes
        }

        public static ShutterEffect ParseEffect(string effect)
        {
            if (Regex.IsMatch(effect, "closed", RegexOptions.IgnoreCase))
                return ShutterEffect.Closed;

            if (Regex.IsMatch(effect, "strobe", RegexOptions.IgnoreCase))
                return ShutterEffect.Strobe;

            if (Regex.IsMatch(effect, "pulse", RegexOptions.IgnoreCase))
                return ShutterEffect.Pulse;

            if (Regex.IsMatch(effect, "rampup", RegexOptions.IgnoreCase))
                return ShutterEffect.RampUp;

            if (Regex.IsMatch(effect, "rampdown", RegexOptions.IgnoreCase))
                return ShutterEffect.RampDown;

            if (Regex.IsMatch(effect, "rampupdown", RegexOptions.IgnoreCase))
                return ShutterEffect.RampUpDown;

            if (Regex.IsMatch(effect, "lightning", RegexOptions.IgnoreCase))
                return ShutterEffect.Lightning;

            if (Regex.IsMatch(effect, "spikes", RegexOptions.IgnoreCase))
                return ShutterEffect.Spikes;

            return ShutterEffect.Open;
        }

        #endregion

        #region Properties

        public enum Entity
        {
            Speed,
            RotationSpeed,
            Time,
            //Distance,
            Brightness,
            ColorTemperature,
            //FogOutput,
            RotationAngle,
            BeamAngle,
            //SwingAngle,
            Parameter,
            SlotNumber,
            Percent,
            //Insertion,
            //IrisPercent
        }

        public static float GetFloatProperty(JSONObject capability, string propertyName, Entity entity, int value)
        {
            PropertyParser parser = null;

            if (entity == Entity.Speed)
                parser = ParseSpeed;
            else if (entity == Entity.RotationSpeed)
                parser = ParseRotationSpeed;
            else if (entity == Entity.Time)
                parser = ParseTime;
            else if (entity == Entity.Brightness)
                parser = ParseBrightness;
            else if (entity == Entity.ColorTemperature)
                parser = ParseColorTemperature;
            else if (entity == Entity.RotationAngle)
                parser = ParseRotationAngle;
            else if (entity == Entity.BeamAngle)
                parser = ParseBeamAngle;
            else if (entity == Entity.Parameter)
                parser = ParseParameter;
            else if (entity == Entity.SlotNumber)
                parser = ParseSlotNumber;
            else if (entity == Entity.Percent)
                parser = ParsePercent;

            if (parser == null)
                return 0;

            if (capability[propertyName])
                return parser(capability[propertyName]);

            float start = 0;
            float end = 1;
            if (capability[propertyName + "Start"])
            {
                start = parser(capability[propertyName + "Start"]);
                end = parser(capability[propertyName + "End"]);
            }

            int dmxStart = 0;
            int dmxEnd = 255;
            if (capability["dmxRange"])
            {
                dmxStart = capability["dmxRange"][0];
                dmxEnd = capability["dmxRange"][1];
            }

            return start + 1f / (dmxEnd - dmxStart) * (value - dmxStart) * (end - start);
        }

        public static int ParseChannelDefault(JSONObject channel)
        {
            string stringValue = channel["defaultValue"];
            if (stringValue == null)
                return 0;

            // TODO: proper fine channel handling

            if (Regex.IsMatch(stringValue, "%", RegexOptions.IgnoreCase))
                return (int)(System.Single.Parse(Regex.Replace(stringValue, "%", "", RegexOptions.IgnoreCase)) / 100 * 255);

            int value = System.Int32.Parse(stringValue);
            if (channel["fineChannelAliases"] != null)
                value >>= 8;

            return value;
        }

        #endregion
    }
}
