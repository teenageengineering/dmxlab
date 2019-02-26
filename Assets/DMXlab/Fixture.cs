using UnityEngine;

namespace DMXlab
{
    public partial class Fixture : MonoBehaviour
    {
        public static int kMaxNumChannels = 128;

        public int startAdress;
        public int numChannels;

        public DMXDriver dmxDriver;

        [SerializeField]
        byte[] _values = new byte[kMaxNumChannels];

        public void SetChannelValue(int channelIndex, byte value)
        {
            if (channelIndex < 0 || channelIndex >= kMaxNumChannels) return;

            // TODO: not always?
            _values[channelIndex] = value;

            UpdateSceneObject(channelIndex);

            int adress = startAdress + channelIndex;
            if (adress > 512) return;

            if (dmxDriver != null)
                dmxDriver[adress] = value;
        }

        public byte GetChannelValue(int channelIndex)
        {
            if (channelIndex < 0 || channelIndex >= kMaxNumChannels) return 0;

            return _values[channelIndex];
        }
    }
}
