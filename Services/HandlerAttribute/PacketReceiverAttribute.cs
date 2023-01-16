namespace NetLib.Handlers.HandlerAttribute;
public class PacketReceiverAttribute : PacketHandlerAttribute
{
    public PacketReceiverAttribute(Type packetType) : base(packetType)
    {
    }
    
}