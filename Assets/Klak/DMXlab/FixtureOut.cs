using UnityEngine;
using Klak.Wiring;
using DMXlab;

namespace Klak.DMX
{
    [AddComponentMenu("Klak/Wiring/Output/DMX/Fixture Out")]
    public class FixtureOut : NodeBase
    {
        #region Editable properties

        [SerializeField]
        Fixture _fixture;

        [SerializeField]
        int _channel;

        [SerializeField]
        string _templateChannel;

        [SerializeField]
        string _pixelKey;

        [SerializeField]
        bool _selectCapability;

        [SerializeField]
        string _capabilityName;

        #endregion

        #region Node I/O

        [Inlet]
        public float channel {
            set {
                if (!enabled || _selectCapability)
                    return;

                _channel = (int)Mathf.Clamp(value, 0, (float)_fixture.numChannels);
            }
        }

        [Inlet]
        public float normalizedValue {
            set {
                if (!enabled || _fixture == null)
                    return;

                float normalizedValue = Mathf.Clamp(value, 0, 1);
                _fixture.SetChannelValue(_channel, System.Convert.ToByte(normalizedValue * 255));
            }
        }

        [Inlet]
        public float rawValue {
            set {
                if (!enabled || _fixture == null)
                    return;

                float rawValue = Mathf.Clamp(value, 0, 255);
                _fixture.SetChannelValue(_channel, System.Convert.ToByte(rawValue));
            }
        }

        #endregion
    }
}
