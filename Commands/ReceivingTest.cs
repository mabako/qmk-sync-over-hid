using System.Windows.Forms;

namespace LilyHid.Commands
{
    internal class ReceivingTest : ICommand
    {
        public void Register(QmkCommunication qmkCommunication)
        {
            qmkCommunication.OnCommandReceived += OnCommandReceived;
        }

        private void OnCommandReceived(object sender, QmkCommunication.CommandEventArgs e)
        {
            if (e.CommandId == QmkReceivedCommandId.Test)
            {
                MessageBox.Show("Test command triggered", "QMK Sync");
            }
        }
    }
}
