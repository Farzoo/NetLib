using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using NetLib.Packets;

namespace NetLib.Server;

public class UdpServer : BaseServer
{
    private ISet<EndPoint> ClientsEndPoints { get; } = new HashSet<EndPoint>();
    public UdpServer(IPEndPoint hostIp, IPacketSerializer packetSerializer) : base(hostIp, SocketType.Dgram, ProtocolType.Udp, packetSerializer)
    {
        this.Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
    }

    protected override void ListenConnection()
    {
        /*Console.WriteLine($"Waiting for connection...");
        byte[] buffer = new byte[IPacket.MaxPacketSize];
        EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
        while (this.IsRunning)
        {
            this.Socket.ReceiveFrom(buffer, SocketFlags.Peek, ref remoteEndPoint);
            if (this.ClientsEndPoints.Add(remoteEndPoint))
            {
                Console.WriteLine($"New client connected: {remoteEndPoint}");
                
                this.ConnectClient(new UdpClient(this.Socket, remoteEndPoint, this.PacketSerializer));
                this.Clients.Last().StartListening();
                Console.WriteLine($"Received packet from {remoteEndPoint}");
            }
        }*/
    }
}