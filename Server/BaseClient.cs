using System.Collections.Immutable;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using NetLib.Packets;

namespace NetLib.Server;

public abstract class BaseClient
{
    protected IPacketSerializer PacketSerializer { get; }
    public abstract bool IsConnected { get; protected set; }

    public delegate void OnReceiveHandler(BaseClient baseClient, byte[] data);
    private event OnReceiveHandler? OnReceive;

    public delegate void OnSendHandler(BaseClient baseClient, BasePacket basePacket);
    protected event OnSendHandler? OnSend;
    
    public delegate void OnDisconnectHandler(BaseClient baseClient);
    private event OnDisconnectHandler? OnDisconnect;
    public Guid Id { get; } = Guid.NewGuid();
    
    protected static readonly int MaxPacketSize = 16384;

    public bool IsListening { get; private set; } = false;

    protected BaseClient(IPacketSerializer packetSerializer)
    {
        this.PacketSerializer = packetSerializer;
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

    public abstract void SendPacket<T>(T packet) where T : BasePacket;

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

    protected virtual void InvokeOnSend(BasePacket basePacket)
    {
        OnSend?.Invoke(this, basePacket);
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