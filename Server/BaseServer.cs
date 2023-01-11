using System.Net;
using System.Net.Sockets;
using NetLib.Packets;
using NetLib.Packets.Shared;

namespace NetLib.Server;

public abstract class BaseServer : IClientEvent
{
    protected Socket Socket { get; }
    
    protected IPEndPoint HostIpEndPoint { get; }
    protected ISet<BaseClient> Clients { get; }
    public bool IsRunning { get; private set; }
    
    protected IPacketMapper PacketMapper { get; }
    
    protected IPacketSerializer PacketSerializer { get; }
    
    protected event IClientEvent.ClientConnectedHandler? ClientConnected;

    protected event IClientEvent.ClientDisconnectedHandler? ClientDisconnected;

    protected BaseServer(IPEndPoint hostIp, SocketType socketType, ProtocolType protocolType, IPacketMapper packetMapper, IPacketSerializer packetSerializer)
    {
        this.HostIpEndPoint = hostIp;
        this.Socket = new Socket(AddressFamily.InterNetworkV6, socketType, protocolType);
        this.Socket.DualMode = true;
        this.Socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
        this.Clients = new HashSet<BaseClient>();
        this.PacketMapper = packetMapper;
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

    protected virtual void DisconnectClient(BaseClient baseClient)
    {
        this.Clients.Remove(baseClient);
        baseClient.UnregisterOnDisconnect(this.DisconnectClient);
        this.ClientDisconnected?.Invoke(baseClient);
    }

    protected virtual void ConnectClient(BaseClient baseClient)
    {
        this.Clients.Add(baseClient);
        baseClient.RegisterOnDisconnect(this.DisconnectClient);
        this.ClientConnected?.Invoke(baseClient);
    }

    public virtual void Broadcast<T>(T packet, BaseClient? except = null) where T : PacketBase
    {

        foreach (BaseClient client in this.Clients)
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