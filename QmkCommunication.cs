using HidSharp;
using HidSharp.Reports;
using HidSharp.Reports.Encodings;
using HidSharp.Reports.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Timer = System.Timers.Timer;

namespace LilyHid
{
    internal class QmkCommunication : IDisposable
    {
        private const int VENDOR_ID = 0x04D8;
        private const int PRODUCT_ID = 0xEB2D;
        private const uint USAGE_PAGE = 0xFF60;
        private const uint USAGE = 0x61;

        private enum Mode : byte
        {
            SendCommand = 0xC3,
            ReceiveCommandResponse = 0xC4,
            ReceiveCommand = 0xC5,
        }

        private HidDevice _device;
        private Timer _timer;
        private HidStream _stream;
        private HidDeviceInputReceiver _inputReceiver;

        public void Start()
        {
            _timer = new Timer
            {
                Interval = 5000,
                Enabled = true,
                AutoReset = true,
            };
            _timer.Elapsed += (sender, e) => Update();
            _timer.Start();

            Update();
            DeviceList.Local.Changed += (sender, e) => OnDeviceListChanged();
        }

        public event EventHandler<ConnectedEventArgs> OnConnected;
        public event EventHandler OnDisconnected;
        public event EventHandler<CommandEventArgs> OnCommandReceived;

        private void Update()
        {
            if (_stream == null || _inputReceiver == null || !_inputReceiver.IsRunning)
            {
                Reset();
                Initialize();
            }
        }

        private void OnDeviceListChanged()
        {
            HidDevice foundDevice = FindKeyboard();
            if (foundDevice != null && _device == null)
            {
                Initialize();
            }
            else if (foundDevice == null && _device != null)
            {
                Reset();
            }
        }

        private void Reset()
        {
            _inputReceiver = null;

            if (_stream != null)
            {
                try { _stream.Dispose(); }
                catch { }

                OnDisconnected?.Invoke(this, EventArgs.Empty);
                _stream = null;
            }

            _device = null;
        }

        private void Initialize()
        {
            _device = FindKeyboard();
            if (_device == null)
            {
                return;
            }

            var reportDescriptor = _device.GetReportDescriptor();
            if (_device.TryOpen(out _stream))
            {
                _stream.ReadTimeout = Timeout.Infinite;

                _inputReceiver = reportDescriptor.CreateHidDeviceInputReceiver();
                if (_inputReceiver != null)
                {
                    _inputReceiver.Received += OnInputReceived;
                    _inputReceiver.Stopped += (sender, e) => Reset();
                    _inputReceiver.Started += (sender, e) => OnConnected?.Invoke(this, new ConnectedEventArgs(_device.GetFriendlyName()));
                    _inputReceiver.Start(_stream);
                }
            }
        }

        private HidDevice FindKeyboard()
        {
            return DeviceList.Local.GetHidDevices(VENDOR_ID, PRODUCT_ID).Where(device =>
            {
                try
                {
                    var rawReportDescriptor = device.GetRawReportDescriptor();
                    var decodedItems = EncodedItem.DecodeItems(rawReportDescriptor, 0, rawReportDescriptor.Length);
                    return decodedItems.Any(i => i.ItemType == ItemType.Global && i.TagForGlobal == GlobalItemTag.UsagePage && i.DataValue == USAGE_PAGE) &&
                        decodedItems.Any(i => i.ItemType == ItemType.Local && i.TagForLocal == LocalItemTag.Usage && i.DataValue == USAGE);
                }
                catch (Exception)
                {
                    return false;
                }
            }).SingleOrDefault();
        }

        private void OnInputReceived(object sender, EventArgs e)
        {
            var buffer = new byte[_device.GetMaxInputReportLength()];
            while (_inputReceiver.TryRead(buffer, 0, out _))
            {
                if ((Mode)buffer[1] == Mode.ReceiveCommand)
                {
                    var payload = new byte[_device.GetMaxInputReportLength() - 3];
                    Array.Copy(buffer, 3, payload, 0, payload.Length);
                    OnCommandReceived?.Invoke(this, new CommandEventArgs((QmkReceivedCommandId)buffer[2], payload));
                }
            }
        }

        public bool SendCommand(QmkSendCommandId commandId, params IEnumerable<byte>[] payload)
        {
            if (_stream == null)
                return false;

            var buffer = new byte[_device.GetMaxOutputReportLength()];
            buffer[1] = (byte)Mode.SendCommand;
            buffer[2] = (byte)commandId;

            var mergedPayload = payload.SelectMany(x => x).ToArray();
            Array.Copy(mergedPayload.ToArray(), 0, buffer, 3, mergedPayload.Length);
            try
            {
                _stream.Write(buffer);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Dispose()
        {
            _timer?.Stop();

            Reset();
        }

        public class ConnectedEventArgs : EventArgs
        {
            public ConnectedEventArgs(string deviceName)
            {
                DeviceName = deviceName;
            }

            public string DeviceName { get; }
        }

        public class CommandEventArgs : EventArgs
        {
            public CommandEventArgs(QmkReceivedCommandId commandId, byte[] payload)
            {
                CommandId = commandId;
                Payload = payload;
            }

            public QmkReceivedCommandId CommandId { get; }
            public byte[] Payload { get; }
        }
    }
}
