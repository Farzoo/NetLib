namespace NetLib.Server;

public interface IClientEvent
{
    public delegate void ClientConnectedHandler(IClient<BaseClient> client);
    public delegate void ClientDisconnectedHandler(IClient<BaseClient> client);
    public void OnClientDisconnected(ClientDisconnectedHandler handler);
    public void OnClientConnected(ClientConnectedHandler handler);
}