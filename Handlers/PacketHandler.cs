using NetLib.Packets.Shared;
using NetLib.Server;

namespace NetLib.Handlers;

public abstract class PacketHandler : IPacketReceivedHandler, IPacketSentHandler
{
    public abstract void OnPacketReceived(BaseClient baseClient, PacketBase packet);
    public abstract void OnPacketSent(BaseClient baseClient, PacketBase packet);
    public abstract ushort PacketId { get; }
}

public interface IPacketReceivedHandler
{
    void OnPacketReceived(BaseClient baseClient, PacketBase packet);
    ushort PacketId { get; }
}

public interface IPacketSentHandler
{
    void OnPacketSent(BaseClient baseClient, PacketBase packet);
    ushort PacketId { get; }
}


