using NetLib.Packets.Shared;
using NetLib.Server;

namespace NetLib.Handlers.Server;

public class PacketHandlerServerManager : IPacketHandlerManager
{
    private IDictionary<ushort, IList<IPacketReceivedHandler>> ReceivedHandlers { get; } = new Dictionary<ushort, IList<IPacketReceivedHandler>>();
    private IDictionary<ushort, IList<IPacketSentHandler>> SentHandlers { get; } = new Dictionary<ushort, IList<IPacketSentHandler>>();

    private IPacketSerializer Serializer { get; }

    private IClientEvent ClientEventManager { get; }
    
    public PacketHandlerServerManager(IClientEvent clientEventManager, IPacketSerializer serializer)
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

    private void OnConnected(BaseClient baseBaseClient)
    {
        Console.WriteLine("Nouvelle connexion");
        baseBaseClient.RegisterOnReceive(this.OnReceive);
        baseBaseClient.RegisterOnSend(this.OnSend);
    }

    private void OnDisconnected(BaseClient baseBaseClient)
    {
        baseBaseClient.UnregisterOnReceive(this.OnReceive);
        baseBaseClient.UnregisterOnSend(this.OnSend);
    }

    private void OnReceive(BaseClient client, byte[] data)
    {
        PacketBase packet = this.Serializer.Deserialize(data);
        
        this.ReceivedHandlers.TryGetValue(packet.Id, out var handlers);
        if(handlers is null) return;
        
        foreach (var packetReceivedHandler in handlers)
        {
            packetReceivedHandler.OnPacketReceived(client, packet);    
        }
    }

    private void OnSend(BaseClient client, PacketBase packetBase)
    {
        this.SentHandlers.TryGetValue(packetBase.Id, out var handlers);
        if(handlers is null) return;

        foreach (var packetSentHandler in handlers)
        {
            packetSentHandler.OnPacketSent(client, packetBase);    
        }
    }
}