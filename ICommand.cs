using Microsoft.Extensions.Configuration;

namespace LilyHid
{
    internal interface ICommand
    {
        void Register(QmkCommunication qmkCommunication);
        void Configure(IConfigurationSection configurationSection) { }
    }
}
