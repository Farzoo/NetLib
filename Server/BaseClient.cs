using System.Net.Sockets;
using NetLib.Packets.Shared;

namespace NetLib.Server;

public abstract class BaseClient
{
    protected IPacketSerializer PacketSerializer { get; }
    public abstract bool IsConnected { get; protected set; }

    public delegate void OnReceiveHandler(BaseClient baseClient, byte[] data);
    private event OnReceiveHandler? OnReceive;

    public delegate void OnSendHandler(BaseClient baseClient, PacketBase packet);
    protected event OnSendHandler? OnSend;
    
    public delegate void OnDisconnectHandler(BaseClient baseClient);
    private event OnDisconnectHandler? OnDisconnect;
    public Guid Id { get; } = Guid.NewGuid();

    public bool IsListening { get; private set; } = false;

    protected BaseClient(IPacketSerializer packetSerializer)
    {
        this.PacketSerializer = packetSerializer;
        // Start listening for incoming data asynchronously until the socket is closed
    }

    public void StartListening()
    {
        if(!this.IsListening)
        {
            this.IsListening = true;
            Task.Factory.StartNew(this.ListenAsync);
        }
    }
    protected abstract void ListenAsync();

    public abstract void SendPacket<T>(T packet) where T : PacketBase;

    public abstract void Disconnect();

    public void RegisterOnReceive(OnReceiveHandler handler)
    {
        this.OnReceive += handler;
    }
    
    public void RegisterOnSend(OnSendHandler? handler)
    {
        this.OnSend += handler;
    }
    
    public void UnregisterOnReceive(OnReceiveHandler handler)
    {
        this.OnReceive -= handler;
    }
    
    public void UnregisterOnSend(OnSendHandler? handler)
    {
        this.OnSend -= handler;
    }
    
    public void RegisterOnDisconnect(OnDisconnectHandler handler)
    {
        this.OnDisconnect += handler;
    }
    
    public void UnregisterOnDisconnect(OnDisconnectHandler handler)
    {
        this.OnDisconnect -= handler;
    }

    protected virtual void InvokeOnSend(PacketBase packet)
    {
        OnSend?.Invoke(this, packet);
    }

    protected virtual void InvokeOnDisconnect()
    {
        OnDisconnect?.Invoke(this);
    }

    protected virtual void InvokeOnReceive(byte[] data)
    {
        OnReceive?.Invoke(this, data);
    }
}