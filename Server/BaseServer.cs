using System.Net;
using System.Net.Sockets;
using NetLib.Packets;

namespace NetLib.Server;

public abstract class BaseServer : IClientEvent
{
    protected Socket Socket { get; }
    
    protected IPEndPoint HostIpEndPoint { get; }
    protected ISet<IClient<BaseClient>> Clients { get; }
    public bool IsRunning { get; private set; }

    protected IPacketSerializer PacketSerializer { get; }
    
    protected event IClientEvent.ClientConnectedHandler? ClientConnected;

    protected event IClientEvent.ClientDisconnectedHandler? ClientDisconnected;

    protected BaseServer(IPEndPoint hostIp, SocketType socketType, ProtocolType protocolType, IPacketSerializer packetSerializer)
    {
        this.HostIpEndPoint = hostIp;
        this.Socket = new Socket(AddressFamily.InterNetworkV6, socketType, protocolType);
        this.Socket.DualMode = true;
        this.Socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
        this.Clients = new HashSet<IClient<BaseClient>>();
        this.PacketSerializer = packetSerializer;
    }

    public virtual void Start()
    {
        if(this.IsRunning) throw new InvalidOperationException("Server is already started");
        this.Socket.Bind(this.HostIpEndPoint);
        this.IsRunning = true;
        Console.WriteLine($"Server has started on {this.Socket.LocalEndPoint}");
        Thread listenThread = new Thread(this.ListenConnection);
        listenThread.Start();
    }

    protected abstract void ListenConnection();

    protected virtual void DisconnectClient(IClient<BaseClient> client)
    {
        this.Clients.Remove(client);
        client.UnregisterOnDisconnect(this.DisconnectClient);
        this.ClientDisconnected?.Invoke(client);
    }

    protected virtual void ConnectClient(IClient<BaseClient> client)
    {
        this.Clients.Add(client);
        client.RegisterOnDisconnect(this.DisconnectClient);
        this.ClientConnected?.Invoke(client);
    }

    public virtual void Broadcast<TPacket>(TPacket packet, IClient<BaseClient>? except = null) where TPacket : BasePacket
    {

        foreach (IClient<BaseClient> client in this.Clients)
        {
            if (client != except)
            {
                client.SendPacket(packet);
            }
        }
    }
    
    public void OnClientConnected(IClientEvent.ClientConnectedHandler handler)
    {
        this.ClientConnected += handler;
    }
    
    public void OnClientDisconnected(IClientEvent.ClientDisconnectedHandler handler)
    {
        this.ClientDisconnected += handler;
    }
}