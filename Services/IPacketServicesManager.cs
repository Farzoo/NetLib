using NetLib.Server;

namespace NetLib.Handlers;

public interface IPacketServicesManager<U, V>
    where U : Delegate
    where V : Delegate
{
    IPacketServicesManager<U, V> RegisterPacketHandler<T>(T packetHandler);
}