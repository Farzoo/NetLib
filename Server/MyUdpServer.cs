using System.Net;
using NetLib.Handlers;
using NetLib.Packets;
using NetLib.Packets.Shared;

namespace NetLib.Server;

public class MyUdpServer : UdpServer
{
    private PacketHandlerManager PacketHandlerManager { get; }
    public MyUdpServer(IPacketMapper packetMapper, IPEndPoint hostIp) : base(hostIp, packetMapper, new PacketSerializer(packetMapper))
    {
        this.PacketHandlerManager = new PacketHandlerManager(this, this.PacketSerializer);
        this.PacketHandlerManager.RegisterPacketReceivedHandler(new LoginHandler());
        this.PacketHandlerManager.RegisterPacketReceivedHandler(new TimeoutHandler());
        this.PacketHandlerManager.RegisterPacketReceivedHandler(new VoiceDataHandler());
    }
}