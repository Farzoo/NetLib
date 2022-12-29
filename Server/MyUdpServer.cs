using System.Net;
using NetLib.Handlers;
using NetLib.Handlers.Server;
using NetLib.Packets;
using NetLib.Packets.Shared;

namespace NetLib.Server;

public class MyUdpServer : UdpServer
{
    private PacketHandlerServerManager PacketHandlerServerManager { get; }
    public MyUdpServer(IPacketMapper packetMapper, IPEndPoint hostIp) : base(hostIp, packetMapper, new PacketSerializer(packetMapper))
    {
        this.PacketHandlerServerManager = new PacketHandlerServerManager(this, this.PacketSerializer);
        this.PacketHandlerServerManager.RegisterPacketReceivedHandler(new LoginHandler());
        this.PacketHandlerServerManager.RegisterPacketReceivedHandler(new TimeoutHandler());
    }
}