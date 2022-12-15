using NetLib.Packets.Client;
using NetLib.Packets.Shared;
using NetLib.Server;

namespace NetLib.Handlers;

public class LoginHandler : IPacketReceivedHandler
{
    public void OnPacketReceived(BaseClient baseBaseClient, PacketBase packet)
    {
        if(packet is LoginPacket loginPacket)
        {
            var username = loginPacket.Username;
            var password = loginPacket.Password;
            // Do something with the username and password
            Console.WriteLine($"Username: {username} Password: {password} from {baseBaseClient.Id}");
        }
    }

    public ushort PacketId => (ushort) PacketType.Login;
}