using System;

namespace LilyHid.Commands
{
    internal class Clock : ICommand
    {
        public void Register(QmkCommunication qmkCommunication)
        {
            qmkCommunication.OnConnected += OnConnected;
        }

        private void OnConnected(object sender, QmkCommunication.ConnectedEventArgs e)
        {
            if (sender is QmkCommunication qmkCommunication)
            {
                int millis = (int)DateTime.Now.TimeOfDay.TotalMilliseconds;
                byte[] milliBytes = BitConverter.GetBytes(millis);
                Array.Reverse(milliBytes);

                qmkCommunication.SendCommand(QmkSendCommandId.SetClock, milliBytes);
            }
        }
    }
}
