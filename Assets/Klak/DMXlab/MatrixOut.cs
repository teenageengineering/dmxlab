using UnityEngine;
using Klak.Wiring;
using DMXlab;

namespace Klak.DMX
{
    [AddComponentMenu("Klak/Wiring/Output/DMX/Matrix Out")]
    public class MatrixOut : NodeBase
    {
        #region Editable properties

        [SerializeField]
        Fixture _fixture;

        [SerializeField]
        int _pixelIndex;

        [SerializeField]
        int _pixelChannel;

        #endregion

        #region Node I/O

        [Inlet]
        public float pixelIndex {
            set {
                if (!enabled || _fixture == null)
                    return;

                _pixelIndex = Mathf.Clamp((int)value, 0, _fixture.pixelKeys.Count - 1);
            }
        }

        [Inlet]
        public float pixelChannel {
            set {
                if (!enabled || _fixture == null)
                    return;

                _pixelChannel = Mathf.Clamp((int)value, 0, _fixture.templateChannelNames.Count - 1);
            }
        }

        [Inlet]
        public float normalizedValue {
            set {
                if (!enabled || _fixture == null)
                    return;

                value = Mathf.Clamp(value, 0, 1);

                string channelName = FixtureLibrary.ExpandTemplateChannelName(_fixture.templateChannelNames[_pixelChannel], _fixture.pixelKeys[_pixelIndex]);
                int channelIndex = _fixture.channelNames.IndexOf(channelName);

                _fixture.SetChannelValue(channelIndex, System.Convert.ToByte(value * 255));
            }
        }

        [Inlet]
        public float rawValue {
            set {
                if (!enabled || _fixture == null)
                    return;

                value = Mathf.Clamp(value, 0, 255);

                string channelName = FixtureLibrary.ExpandTemplateChannelName(_fixture.templateChannelNames[_pixelChannel], _fixture.pixelKeys[_pixelIndex]);
                int channelIndex = _fixture.channelNames.IndexOf(channelName);

                _fixture.SetChannelValue(channelIndex, System.Convert.ToByte(value));
            }
        }

        #endregion
    }
}
