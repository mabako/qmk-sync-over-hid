using System;
using System.Timers;

namespace LilyHid.Commands
{
    internal class Clock : ICommand
    {
        QmkCommunication _qmkCommunication;

        public void Register(QmkCommunication qmkCommunication)
        {
            _qmkCommunication = qmkCommunication;
            _qmkCommunication.OnConnected += SendClock;

            var timer = new Timer
            {
                Interval = TimeSpan.FromMinutes(15).TotalMilliseconds,
                Enabled = true,
                AutoReset = true,
            };
            timer.Elapsed += SendClock;
            timer.Start();
        }

        private void SendClock(object sender, EventArgs e)
        {
            int millis = (int)DateTime.Now.TimeOfDay.TotalMilliseconds;
            _qmkCommunication.SendCommand(QmkSendCommandId.SetClock, BitConverter.GetBytes(millis));
        }
    }
}
