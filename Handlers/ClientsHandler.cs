using NetLib.Server;

namespace NetLib.Handlers;

public class ClientsHandler : IClientEvent
{
    private List<BaseClient> Clients { get; } = new List<BaseClient>();
    private event IClientEvent.ClientDisconnectedHandler? ClientDisconnected;
    private event IClientEvent.ClientConnectedHandler? ClientConnected;

    public ClientsHandler()
    {
        
    }
    
    public void ConnectClient(BaseClient client)
    {
        this.Clients.Add(client);
        this.ClientConnected?.Invoke(client);
    }
    
    public void DisconnectClient(BaseClient client)
    {
        if (this.Clients.Remove(client))
        {
            this.ClientDisconnected?.Invoke(client);
        }
    }
    public void OnClientDisconnected(IClientEvent.ClientDisconnectedHandler handler)
    {
        this.ClientDisconnected += handler;
    }

    public void OnClientConnected(IClientEvent.ClientConnectedHandler handler)
    {
        this.ClientConnected += handler;
    }
}