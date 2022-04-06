using HidSharp;
using HidSharp.Reports.Encodings;
using System;
using System.Linq;
using System.Net;
using System.Threading;

namespace LilyHid
{
    class Program
    {
        static void Main(string[] args)
        {
            Update();

            DeviceList.Local.Changed += (sender, e) => Update();

            var myTimer = new System.Timers.Timer(10 * 60 * 1000);
            myTimer.Elapsed += (sender, e) => Update();
            myTimer.Start();
            
            new ManualResetEvent(false).WaitOne();
        }

        private static void Update()
        {
            var devices = DeviceList.Local.GetHidDevices(0x04D8, 0xEB2D).Where(device =>
            {
                try
                {
                    var rawReportDescriptor = device.GetRawReportDescriptor();
                    var decodedItems = EncodedItem.DecodeItems(rawReportDescriptor, 0, rawReportDescriptor.Length);
                    return decodedItems.Any(i => i.ItemType == ItemType.Global && i.TagForGlobal == GlobalItemTag.UsagePage && i.DataValue == 0xFF60) &&
                        decodedItems.Any(i => i.ItemType == ItemType.Local && i.TagForLocal == LocalItemTag.Usage && i.DataValue == 0x61);
                }
                catch (Exception)
                {
                    return false;
                }
            });
            foreach (var device in devices)
            {
                Console.WriteLine(device.DevicePath);

                var reportDescriptor = device.GetReportDescriptor();
                if (device.TryOpen(out var hidStream))
                {
                    hidStream.ReadTimeout = Timeout.Infinite;

                    using (hidStream)
                    {
                        int millis = (int)DateTime.Now.TimeOfDay.TotalMilliseconds;
                        byte[] milliBytes = BitConverter.GetBytes(millis);
                        Array.Reverse(milliBytes);
                        var buffer = new byte[device.GetMaxOutputReportLength()];
                        buffer[1] = 0x01;
                        Array.Copy(milliBytes, 0, buffer, 2, milliBytes.Length);
                        hidStream.Write(buffer);
                    }
                }
            }
        }
    }
}
