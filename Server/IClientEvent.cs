namespace NetLib.Server;

public interface IClientEvent
{
    public delegate void ClientConnectedHandler(BaseClient baseClient);
    public delegate void ClientDisconnectedHandler(BaseClient baseClient);
    public void OnClientDisconnected(ClientDisconnectedHandler handler);
    public void OnClientConnected(ClientConnectedHandler handler);
}