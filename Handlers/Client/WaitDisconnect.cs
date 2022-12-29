using NetLib.Server;

namespace NetLib.Handlers.Client;

public class WaitDisconnect
{
    private ManualResetEventSlim _waitHandle = new ManualResetEventSlim(false);

    public WaitDisconnect(BaseClient client)
    {
        client.RegisterOnDisconnect(this.OnDisconnect);
        this._waitHandle.Wait();
    }

    private void OnDisconnect(BaseClient client)
    {
        this._waitHandle.Set();
    }
}