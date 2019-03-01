using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using UnityEngine;

namespace DMXlab
{
    [ExecuteInEditMode]
    public class Driver : MonoBehaviour
    {
        const int N_DMX_CHANNELS = 512;

        // TODO: support more protocols

        const byte DMX_PRO_HEADER_SIZE = 4;
        const byte DMX_PRO_START_MSG = 0x7E;
        const byte DMX_PRO_LABEL_DMX = 6;
        const byte DMX_PRO_LABEL_SERIAL_NUMBER = 10;
        const byte DMX_PRO_START_CODE = 0;
        const byte DMX_PRO_START_CODE_SIZE = 1;
        const byte DMX_PRO_END_MSG = 0xE7;

        const int DMX_PRO_DATA_INDEX_OFFSET = 5;
        const int DMX_PRO_MESSAGE_OVERHEAD = 6;

        const int TX_BUFFER_LENGTH = DMX_PRO_MESSAGE_OVERHEAD + N_DMX_CHANNELS;

        #region Public

        public string serialPortName;

        #endregion

        bool _needsRefresh;
        static SerialPort serialPort;
        byte[] TxBuffer = new byte[DMX_PRO_MESSAGE_OVERHEAD + N_DMX_CHANNELS];

        public byte this[int index] {
            get {
                if (index < 1 || index > N_DMX_CHANNELS)
                    return 0;

                return TxBuffer[DMX_PRO_DATA_INDEX_OFFSET + index - 1];
            }
            set {
                if (index < 1 || index > N_DMX_CHANNELS)
                    return;

                TxBuffer[DMX_PRO_DATA_INDEX_OFFSET + index - 1] = value;
                _needsRefresh = true;
            }
        }

        public static string[] GetPortNames()
        {
            int p = (int)System.Environment.OSVersion.Platform;
            List<string> serialPorts = new List<string>();
            serialPorts.Add("");

            if (p == 4 || p == 128 || p == 6)
            {
                string[] ttys = Directory.GetFiles("/dev/", "tty.*");
                foreach (string dev in ttys)
                    serialPorts.Add(Path.GetFileName(dev));
            }

            return serialPorts.ToArray();
        }

        string _prevSerialPortName;
        void OpenSerialPort()
        {
            if (serialPort != null)
                CloseSerialPort();

            if (!string.IsNullOrEmpty(serialPortName))
            {
                string path = "/dev/" + serialPortName;
                serialPort = new SerialPort(path, 57600, Parity.None, 8, StopBits.One);
                serialPort.Open();
            }

            _prevSerialPortName = serialPortName;
        }

        void CloseSerialPort()
        {
            serialPort.Close();
            serialPort.Dispose();
        }

        void Send()
        {
            if (serialPort != null && serialPort.IsOpen)
            {
                serialPort.Write(TxBuffer, 0, TX_BUFFER_LENGTH);
                _needsRefresh = false;
            }
        }

        #region MonoBehaviour

        void Start()
        {
            if (Application.isEditor)
                Application.runInBackground = true;

            TxBuffer[000] = DMX_PRO_START_MSG;
            TxBuffer[001] = DMX_PRO_LABEL_DMX;
            TxBuffer[002] = (byte)(N_DMX_CHANNELS + 1 & 255); ;
            TxBuffer[003] = (byte)((N_DMX_CHANNELS + 1 >> 8) & 255);
            TxBuffer[004] = DMX_PRO_START_CODE;
            TxBuffer[517] = DMX_PRO_END_MSG;
        }

        void Update()
        {
            if (serialPortName != _prevSerialPortName)
                OpenSerialPort();

            if (_needsRefresh)
                Send();
        }

        void OnApplicationQuit()
        {
            for (int i = 0; i < N_DMX_CHANNELS; i++) TxBuffer[DMX_PRO_DATA_INDEX_OFFSET + i] = (byte)0x00;
            Send();

            if (serialPort != null)
                CloseSerialPort();
        }

        #endregion
    }
}

