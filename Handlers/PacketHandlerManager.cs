using NetLib.Packets.Shared;
using NetLib.Server;

namespace NetLib.Handlers;

public class PacketHandlerManager
{
    public IDictionary<ushort, IList<IPacketReceivedHandler>> ReceivedHandlers { get; } = new Dictionary<ushort, IList<IPacketReceivedHandler>>();
    public IDictionary<ushort, IList<IPacketSentHandler>> SentHandlers { get; } = new Dictionary<ushort, IList<IPacketSentHandler>>();

    public IPacketSerializer Serializer { get; }

    private IClientEvent ClientEventManager { get; }
    
    public PacketHandlerManager(IClientEvent clientEventManager, IPacketSerializer serializer)
    {
        this.ClientEventManager = clientEventManager;
        this.ClientEventManager.OnClientConnected(this.OnConnected);
        this.ClientEventManager.OnClientDisconnected(this.OnDisconnected);
        this.Serializer = serializer;
    }
    public void RegisterPacketReceivedHandler(IPacketReceivedHandler handler)
    {
        this.ReceivedHandlers.TryGetValue(handler.PacketId, out var handlers);
        if (handlers == null)
        {
            handlers = new List<IPacketReceivedHandler>();
            this.ReceivedHandlers.Add(handler.PacketId, handlers);
        }
        else
        {
            if (handlers.Any(x => x.GetType() == handler.GetType()))
                throw new ArgumentException($"ReceivedHandlers {handler.GetType().Name} already registered");
        }
        handlers.Add(handler);
    }
    
    public void RegisterPacketSentHandler(IPacketSentHandler handler)
    {
        this.SentHandlers.TryGetValue(handler.PacketId, out var handlers);
        if (handlers == null)
        {
            handlers = new List<IPacketSentHandler>();
            this.SentHandlers.Add(handler.PacketId, handlers);
        }
        else
        {
            if (handlers.Any(x => x.GetType() == handler.GetType()))
                throw new ArgumentException($"SentHandlers {handler.GetType().Name} already registered");
            handlers.Add(handler);
        }
    }
    
    public void OnConnected(BaseClient baseBaseClient)
    {
        Console.WriteLine("Nouvelle connexion");
        baseBaseClient.RegisterOnReceive(this.OnReceive);
    }
    
    public void OnDisconnected(BaseClient baseBaseClient)
    {
        baseBaseClient.UnregisterOnReceive(this.OnReceive);
    }
    
    public void OnReceive(BaseClient baseBaseClient, byte[] data)
    {
        PacketBase packet = this.Serializer.Deserialize(data);
        //Console.WriteLine($"Reception d'un paquet : {packet.GetType().Name}");
        this.ReceivedHandlers.TryGetValue(packet.Id, out var handlers);
        handlers?.ToList().ForEach(x => x.OnPacketReceived(baseBaseClient, packet));
    }
    
    public void OnSend(BaseClient baseBaseClient, PacketBase packetBase)
    {
        this.SentHandlers.TryGetValue(packetBase.Id, out var handlers);
        handlers?.ToList().ForEach(x => x.OnPacketSent(baseBaseClient, packetBase));
    }
}