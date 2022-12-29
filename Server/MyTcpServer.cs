using System.Net;
using System.Net.Sockets;
using NetLib.Handlers;
using NetLib.Handlers.Server;
using NetLib.Packets;
using NetLib.Packets.Shared;

namespace NetLib.Server;

public class MyTcpServer : TcpServer
{
    private PacketHandlerServerManager PacketHandlerServerManager { get; }

    public MyTcpServer(IPacketMapper packetMapper, IPEndPoint hostIp) : base(hostIp, packetMapper, new PacketSerializer(packetMapper))
    {
        this.PacketHandlerServerManager = new PacketHandlerServerManager(this, this.PacketSerializer);
        this.PacketHandlerServerManager.RegisterPacketReceivedHandler(new LoginHandler());
        this.PacketHandlerServerManager.RegisterPacketReceivedHandler(new VoiceDataBroadcastHandler(this));
    }
}