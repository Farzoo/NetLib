namespace NetLib.Handlers;

public interface IPacketHandlerManager
{ 
    void RegisterPacketReceivedHandler(IPacketReceivedHandler handler);
    void RegisterPacketSentHandler(IPacketSentHandler handler);
}