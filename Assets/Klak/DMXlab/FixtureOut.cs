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
        bool _selectCapability;

        [SerializeField]
        string _capabilityName;

        #endregion

        #region Node I/O

        [Inlet]
        public float channel {
            set {
                if (!enabled || _fixture == null)
                    return;

                _channel = Mathf.Clamp((int)value, 0, _fixture.numChannels - 1);
            }
        }

        [Inlet]
        public float normalizedValue {
            set {
                if (!enabled || _fixture == null)
                    return;

                value = Mathf.Clamp(value, 0, 1);
                _fixture.SetChannelValue(_channel, System.Convert.ToByte(value * 255));
            }
        }

        [Inlet]
        public float rawValue {
            set {
                if (!enabled || _fixture == null)
                    return;

                value = Mathf.Clamp(value, 0, 255);
                _fixture.SetChannelValue(_channel, System.Convert.ToByte(value));
            }
        }

        #endregion
    }
}
