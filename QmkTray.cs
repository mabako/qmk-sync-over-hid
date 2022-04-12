using LilyHid.Commands;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace LilyHid
{
    class QmkTray : IDisposable
    {
        private readonly NotifyIcon _notifyIcon = new NotifyIcon
        {
            Text = "Initializing...",
            Visible = true,
            Icon = Resources.Icons.key
        };
        private readonly QmkCommunication _qmkCommunication = new QmkCommunication();
        private readonly List<ICommand> _commands = Assembly.GetExecutingAssembly().GetTypes()
            .Where(type => typeof(ICommand).IsAssignableFrom(type) && !type.IsInterface)
            .Select(type => (ICommand)Activator.CreateInstance(type))
            .ToList();

        public QmkTray()
        {
            InitializeCommandsFromConfig();
            ConfigureContextMenu();
            ConfigureCommunication();
        }

        private void InitializeCommandsFromConfig()
        {
            IConfiguration configuration = new ConfigurationBuilder()
               .AddJsonFile("appsettings.json", optional: true)
               .Build();
            _commands.ForEach(command => command.Configure(configuration.GetSection(command.GetType().Name)));
        }

        private void ConfigureContextMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            ToolStripMenuItem exit = new ToolStripMenuItem("Exit");
            exit.Click += (sender, e) => _notifyIcon.Visible = false;
            menu.Items.Add(exit);
            _notifyIcon.ContextMenuStrip = menu;
        }

        private void ConfigureCommunication()
        {
            _qmkCommunication.OnConnected += (sender, e) => _notifyIcon.Text = $"{e.DeviceName} connected";
            _qmkCommunication.OnDisconnected += (sender, e) => _notifyIcon.Text = "QMK disconnected";
            _commands.ForEach(command => command.Register(_qmkCommunication));
            _qmkCommunication.Start();
        }

        public bool IsNotififyIconVisible => _notifyIcon.Visible;

        public void Dispose()
        {
            _commands.ForEach(command =>
            {
                if (command is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            });
            _qmkCommunication.Dispose();
            _notifyIcon.Dispose();
        }
    }
}
