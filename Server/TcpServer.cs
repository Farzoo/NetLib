using System.Net;
using System.Net.Sockets;
using NetLib.Packets;

namespace NetLib.Server;

public class TcpServer : BaseServer
{
    public TcpServer(IPEndPoint hostIp, IPacketSerializer packetSerializer) : base(hostIp, SocketType.Stream, ProtocolType.Tcp, packetSerializer)
    {
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