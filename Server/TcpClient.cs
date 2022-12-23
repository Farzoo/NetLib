using System.Net;
using System.Net.Sockets;
using NetLib.Packets.Shared;

namespace NetLib.Server;

public class TcpClient : BaseClient
{
    private Socket Socket { get; }
    
    private byte[] _socketBuffer = new byte[PacketBase.MaxPacketSize];

    private int ReadBytes { get; set; } 

    private bool _isConnected;

    public sealed override bool IsConnected
    {
        get => Socket.Connected && this._isConnected;
        protected set => this._isConnected = value;
    }

    public TcpClient(Socket socket, IPacketSerializer packetSerializer) : base(packetSerializer)
    {
        if(socket.ProtocolType != ProtocolType.Tcp)
            throw new ArgumentException($"Socket must be of type TCP. Is {socket.ProtocolType}", nameof(socket));
        this.Socket = socket;
        this.IsConnected = true;
        this.Socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
        // Start listening for incoming data asynchronously until the socket is closed
        Task.Factory.StartNew(ListenAsync);
    }
    
    public TcpClient(IPEndPoint serverEndPoint, IPacketSerializer packetSerializer) : base(packetSerializer)
    {
        this.Socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
        this.Socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
        this.Socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
        this.Socket.Connect(serverEndPoint);
        this.IsConnected = true;
        // Start listening for incoming data asynchronously until the socket is closed
        Task.Factory.StartNew(ListenAsync);
    }

    protected override void ListenAsync()
    {
        while (this.IsConnected)
        {
            try
            {
                this.ReadBytes = this.Socket.Receive(this._socketBuffer);
                Task.Factory.StartNew(this.ReceivePacket).ContinueWith(task =>
                {
                    if (task.IsFaulted) Console.WriteLine(task.Exception);
                });
                //Thread.Sleep(100);
            }
            catch (SocketException socketException)
            {
                this.Disconnect();
            }
        }
    }

    private void ReceivePacket()
    {
        // Copy the received data into a new array of the exact size
        if (this.IsConnected)
        {
            byte[] data = new byte[this.ReadBytes];
            Array.Copy(this._socketBuffer, data, this.ReadBytes);
            this.InvokeOnReceive(data);
        }
        else
        {
            this.Disconnect();
        }
    }
    
    public override void SendPacket<T>(T packet)
    {
        if (this.IsConnected)
        {
            byte[] packetBytes = this.PacketSerializer.Serialize(packet);
            lock(this.Socket)
            {
                this.Socket.Send(packetBytes);
            }
            this.InvokeOnSend(packet);
            
        }
        else
        {
            this.Disconnect();
        }
    }

    public override void Disconnect()
    {
        this.IsConnected = false;
        this.Socket.Close();
        this.InvokeOnDisconnect();
    }

    // toString
    public override string ToString()
    {
        return $"Remote : {this.Socket.RemoteEndPoint} <=>  Local : {this.Socket.LocalEndPoint}";
    }
}