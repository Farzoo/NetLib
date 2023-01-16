namespace NetLib.Handlers.HandlerAttribute;
public class PacketSenderAttribute : PacketHandlerAttribute
{
    public PacketSenderAttribute(Type packetType) : base(packetType)
    {
    }
}