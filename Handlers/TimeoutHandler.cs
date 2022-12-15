using NetLib.Packets.Shared;
using NetLib.Server;

namespace NetLib.Handlers;

public class TimeoutHandler : PacketHandler
{
    public override void OnPacketReceived(BaseClient baseClient, PacketBase packet)
    {
        if (packet is PingPacket timeoutPacket)
        {
            Console.WriteLine($"Received timeout packet from {baseClient.Id}");
            baseClient.SendPacket(packet);
        }
    }

    public override void OnPacketSent(BaseClient baseClient, PacketBase packet)
    {
        throw new NotImplementedException();
    }

    public override ushort PacketId { get; } = (ushort) PacketType.Ping;
}