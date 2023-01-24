using NetLib.Packets;

namespace NetLib.Server;

public interface IClient<TClient> 
    where TClient : IClient<TClient>
{
    public delegate void OnReceiveHandler<in T>(T client, byte[] data);

    public delegate void OnSendHandler<in T>(T client, BasePacket basePacket);

    public delegate void OnDisconnectHandler<in T>(T client);

    public abstract bool IsConnected { get; }
    public bool IsListening { get; }

    public void Connect();
    public void Disconnect();
    
    public void StartListening();
    
    public abstract void SendPacket<T>(T packet) where T : BasePacket;

    public void RegisterOnReceive(OnReceiveHandler<TClient> handler);
    public void RegisterOnSend(OnSendHandler<TClient>? handler);
    public void RegisterOnDisconnect(OnDisconnectHandler<TClient> handler);

    public void UnregisterOnReceive(OnReceiveHandler<TClient> handler);
    public void UnregisterOnSend(OnSendHandler<TClient>? handler);
    public void UnregisterOnDisconnect(OnDisconnectHandler<TClient> handler);

}