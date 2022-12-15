using System.Net;
using System.Net.Sockets;
using NetLib.Packets;
using NetLib.Packets.Shared;

namespace NetLib.Server;

public class TcpServer : BaseServer
{
    public TcpServer(IPEndPoint hostIp, IPacketMapper packetMapper, IPacketSerializer packetSerializer) : base(hostIp, SocketType.Stream, ProtocolType.Tcp, packetMapper, packetSerializer)
    {
        Console.WriteLine($"{this.Socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress)}");
    }

    protected override void ListenConnection()
    {
        Console.WriteLine($"Waiting for connection...");
        this.Socket.Listen(0);

        while (this.IsRunning)
        {
            Socket clientListener = this.Socket.Accept();
            Console.WriteLine($"Client connected: {clientListener.LocalEndPoint} -> {clientListener.RemoteEndPoint}");
            this.ConnectClient(new TcpClient(clientListener, this.PacketSerializer));
            this.Clients.Last().StartListening();
        }
    }
}