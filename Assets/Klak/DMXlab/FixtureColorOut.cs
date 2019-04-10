using UnityEngine;
using Klak.Wiring;
using DMXlab;

namespace Klak.DMX
{
    [AddComponentMenu("Klak/Wiring/Output/DMX/Fixture Color Out")]
    public class FixtureColorOut : NodeBase
    {
        #region Editable properties

        [SerializeField]
        Fixture _fixture;

        [SerializeField]
        int _redChannel;

        [SerializeField]
        int _greenChannel;

        [SerializeField]
        int _blueChannel;

        public enum Mode { Single, Matrix }

        [SerializeField]
        Mode _mode;

        [SerializeField]
        int _pixelIndex;

        #endregion

        #region Node I/O

        [Inlet]
        public Color color {
            set {
                if (!enabled || _fixture == null)
                    return;

                if (_redChannel != -1)
                    _fixture.SetChannelValue(_redChannel, System.Convert.ToByte(value.r * 255));

                if (_greenChannel != -1)
                    _fixture.SetChannelValue(_greenChannel, System.Convert.ToByte(value.g * 255));

                if (_blueChannel != -1)
                    _fixture.SetChannelValue(_blueChannel, System.Convert.ToByte(value.b * 255));

                // TODO: alpha / white
            }
        }

        #endregion
    }
}
