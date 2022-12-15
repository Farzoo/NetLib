using System.Net;
using System.Net.Sockets;
using NetLib.Handlers;
using NetLib.Packets;
using NetLib.Packets.Shared;

namespace NetLib.Server;

public class MyTcpServer : TcpServer
{
    private PacketHandlerManager PacketHandlerManager { get; }

    public MyTcpServer(IPacketMapper packetMapper, IPEndPoint hostIp) : base(hostIp, packetMapper, new PacketSerializer(packetMapper))
    {
        this.PacketHandlerManager = new PacketHandlerManager(this, this.PacketSerializer);
        this.PacketHandlerManager.RegisterPacketReceivedHandler(new LoginHandler());
        this.PacketHandlerManager.RegisterPacketReceivedHandler(new VoiceDataHandler());
    }
}