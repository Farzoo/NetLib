using System.Net;
using System.Net.Sockets;
using NetLib.Packets;
using NetLib.Packets.Shared;

namespace NetLib.Server;

public class UdpClient : BaseClient
{
    public override bool IsConnected { get; protected set; } = true;
    
    private readonly byte[] _buffer = new byte[PacketBase.MaxPacketSize];
    protected Socket ListenSocket { get; }
    protected Socket SendSocket { get; }

    public UdpClient(Socket listenSocket, EndPoint sendIpEndPoint, IPacketSerializer packetSerializer) : base(packetSerializer)
    {
        if(listenSocket.ProtocolType != ProtocolType.Udp)
            throw new ArgumentException("listenSocket must be a UDP socket", nameof(listenSocket));
        this.ListenSocket = listenSocket;
        listenSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, false);
        this.SendSocket = new Socket(this.ListenSocket.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
        this.SendSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        this.SendSocket.Bind(listenSocket.LocalEndPoint);
        this.SendSocket.Connect(sendIpEndPoint);
        Console.WriteLine(this);
        Console.WriteLine($"{this.ListenSocket.ReceiveBufferSize}");
    }
    
    public UdpClient(EndPoint listenIpEndPoint, EndPoint sendIpEndPoint, IPacketSerializer packetSerializer) : base(packetSerializer)
    {
        this.SendSocket = new Socket(listenIpEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
        this.SendSocket.Connect(sendIpEndPoint);
        this.ListenSocket = new Socket(listenIpEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
        this.ListenSocket.Bind(this.SendSocket.LocalEndPoint);
        Console.WriteLine($"{this.ListenSocket.ReceiveBufferSize}");
        Console.WriteLine(this);
    }

    protected override void ListenAsync()
    {
        EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
        while (this.IsConnected)
        {
            this.ListenSocket.ReceiveFrom(this._buffer, SocketFlags.Peek, ref remoteEndPoint);
            if (remoteEndPoint.Equals(this.SendSocket.RemoteEndPoint))
            {
                this.ListenSocket.ReceiveFrom(this._buffer, SocketFlags.None, ref remoteEndPoint);
                this.ReceivePacket();
            }
            //Console.WriteLine("Listening...");
        }
    }
    
    private void ReceivePacket()
    {
        this.InvokeOnReceive(this._buffer);
    }

    public override void SendPacket<T>(T packet)
    {
        //Console.WriteLine($"Sending packet {packet.GetType().Name}");
        this.SendSocket.Send(this.PacketSerializer.Serialize(packet));
    }

    public override void Disconnect()
    {
        this.InvokeOnDisconnect();
    }
    
    // toString
    
    public override string ToString()
    {
        return $"UDP Client:\n SendSocket = Remote {this.SendSocket.RemoteEndPoint} - Local {this.SendSocket.LocalEndPoint}\n ListenSocket =  Local {this.ListenSocket.LocalEndPoint}";
    }
}