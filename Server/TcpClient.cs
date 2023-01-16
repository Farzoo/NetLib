using System.Buffers;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using NetLib.Packets;

namespace NetLib.Server;

public class TcpClient : BaseClient
{
    private Socket Socket { get; }
    
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
    }
    
    public TcpClient(IPEndPoint serverEndPoint, IPacketSerializer packetSerializer) : base(packetSerializer)
    {
        this.Socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
        this.Socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
        this.Socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
        this.Socket.Connect(serverEndPoint);
        this.IsConnected = true;
    }

    protected override void ListenAsync()
    {
        while (this.IsConnected)
        {
            try
            {
                byte[] buffer = ArrayPool<byte>.Shared.Rent(MaxPacketSize);
                // Receive data from the client asynchronously and store it in the buffer
                int readBytes = this.Socket.Receive(buffer);
                
                Task.Factory.StartNew(() => this.ReceivePacket(buffer, readBytes)).ContinueWith(task =>
                {
                    if (task.IsFaulted) Console.WriteLine(task.Exception);
                });
            }
            catch (SocketException socketException)
            {
                this.Disconnect();
            }
        }
    }

    private void ReceivePacket(byte[] buffer, int readBytes)
    {
        // Copy the received data into a new array of the exact size
        if (!this.IsConnected) return;

        this.InvokeOnReceive(buffer.AsSpan(0, readBytes).ToArray());

        ArrayPool<byte>.Shared.Return(buffer);
    }
    
    public override void SendPacket<T>(T packet)
    {
        if (!this.IsConnected) return;

        try
        {
            byte[] packetBytes = this.PacketSerializer.Serialize(packet);
            lock (this.Socket)
            {
                this.Socket.Send(packetBytes);
            }
            this.InvokeOnSend(packet);
        }
        catch (SocketException exception)
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