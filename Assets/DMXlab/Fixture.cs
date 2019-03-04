using UnityEngine;

namespace DMXlab
{
    public partial class Fixture : MonoBehaviour
    {
        public static int kMaxNumChannels = 128;

        public int startAdress;
        public int numChannels;

        public Driver dmxDriver;

        [SerializeField]
        byte[] _values = new byte[kMaxNumChannels];

        public void SetChannelValue(int channelIndex, byte value)
        {
            if (channelIndex < 0 || channelIndex >= kMaxNumChannels) 
                return;

            if (value == _values[channelIndex])
                return;

            _values[channelIndex] = value;

            UpdateChannel(channelIndex);
        }

        public byte GetChannelValue(int channelIndex)
        {
            if (channelIndex < 0 || channelIndex >= kMaxNumChannels) 
                return 0;

            return _values[channelIndex];
        }

        public void RefreshDriver()
        {
            if (dmxDriver == null)
                return;

            for (int i = 0; i < numChannels; i++)
            {
                int adress = startAdress + i;
                if (adress > 512) break;

                dmxDriver.SetValue(adress, _values[i]);
            }
        }

        void Update()
        {
            UpdatePreview();

            RefreshDriver();
        }
    }
}
