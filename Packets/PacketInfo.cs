namespace NetLib.Packets;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class PacketInfo : Attribute
{
    public PacketInfo(ushort id)
    {
        Id = id;
    }

    public ushort Id { get; }
}