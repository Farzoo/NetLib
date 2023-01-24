using NetLib.Packets;

namespace NetLib.Server;

public abstract class BaseClient : IClient<BaseClient>
{
    private event IClient<BaseClient>.OnReceiveHandler<BaseClient>? OnReceive;
    private event IClient<BaseClient>.OnSendHandler<BaseClient>? OnSend;
    private event IClient<BaseClient>.OnDisconnectHandler<BaseClient>? OnDisconnect;
    protected IPacketSerializer PacketSerializer { get; }
    public abstract bool IsConnected { get; protected set; }

    public bool IsListening { get; private set; } = false;

    protected BaseClient(IPacketSerializer packetSerializer)
    {
        this.PacketSerializer = packetSerializer;
    }

    public abstract void Connect();
    public abstract void Disconnect();
    
    public virtual void StartListening()
    {
        if (this.IsListening) return;
        this.IsListening = true;
        Task.Factory.StartNew(this.ListenAsync);
    }

    protected abstract void ListenAsync();
    
    public abstract void SendPacket<T>(T packet) where T : BasePacket;

    public void RegisterOnReceive(IClient<BaseClient>.OnReceiveHandler<BaseClient> handler)
    {
        this.OnReceive += handler;
    }
    
    public void RegisterOnSend(IClient<BaseClient>.OnSendHandler<BaseClient>? handler)
    {
        this.OnSend += handler;
    }
    
    public void UnregisterOnReceive(IClient<BaseClient>.OnReceiveHandler<BaseClient> handler)
    {
        this.OnReceive -= handler;
    }
    
    public void UnregisterOnSend(IClient<BaseClient>.OnSendHandler<BaseClient>? handler)
    {
        this.OnSend -= handler;
    }
    
    public void RegisterOnDisconnect(IClient<BaseClient>.OnDisconnectHandler<BaseClient> handler)
    {
        this.OnDisconnect += handler;
    }
    
    public void UnregisterOnDisconnect(IClient<BaseClient>.OnDisconnectHandler<BaseClient> handler)
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