using System.Net;
using System.Net.Sockets;
using NetLib.Packets.Shared;

namespace NetLib.Server;

public class TcpClient : BaseClient
{
    private Socket Socket { get; }
    
    private readonly byte[] _buffer = new byte[PacketBase.MaxPacketSize];
    
    public override bool IsConnected
    {
        get => Socket.Connected;
        protected set {  }
    }

    public TcpClient(Socket socket, IPacketSerializer packetSerializer) : base(packetSerializer)
    {
        if(socket.ProtocolType != ProtocolType.Tcp)
            throw new ArgumentException($"Socket must be of type TCP. Is {socket.ProtocolType}", nameof(socket));
        this.Socket = socket;
        // Start listening for incoming data asynchronously until the socket is closed
        Task.Factory.StartNew(ListenAsync);
    }
    
    public TcpClient(IPEndPoint serverEndPoint, IPacketSerializer packetSerializer) : base(packetSerializer)
    {
        Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        Socket.Connect(serverEndPoint);
        // Start listening for incoming data asynchronously until the socket is closed
        Task.Factory.StartNew(ListenAsync);
    }

    protected override void ListenAsync()
    {
        while (this.IsConnected)
        {
            this.Socket.Receive(this._buffer);
            this.ReceivePacket();
        }
    }

    private void ReceivePacket()
    {
        this.InvokeOnReceive(this._buffer);
    }
    
    public override void SendPacket<T>(T packet)
    {
        byte[] packetBytes = this.PacketSerializer.Serialize(packet);
        // print the packet bytes to the console
        //Console.WriteLine(BitConverter.ToString(packetBytes));
        this.Socket.Send(packetBytes);
        this.InvokeOnSend(packet);
    }

    public override void Disconnect()
    {
        this.InvokeOnDisconnect();
    }

    // toString
    public override string ToString()
    {
        return $"Remote : {this.Socket.RemoteEndPoint} <=>  Local : {this.Socket.LocalEndPoint}";
    }
}