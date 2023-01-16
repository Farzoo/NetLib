namespace NetLib.Packets;

public interface IPacketSerializer<T> where T : BasePacket
{
    byte[] Serialize(T packet);
    T Deserialize(byte[] data);
    
    ushort GetPacketId(byte[] data);
}

public interface IPacketSerializer
{
    byte[] Serialize(BasePacket basePacket);
    BasePacket Deserialize(byte[] data);
}