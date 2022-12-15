namespace NetLib.Packets.Shared;

public enum PacketType : ushort
{
    Login,
    Logout,
    ChatMessage,
    Register,
    Userlist,
    
    // Server to client
    ServerChatMessage,
    ServerUserlist,
    
    VoiceData,
    
    Ping,
    Unknown
}

public static class PacketTypeExtensions
{
    public static PacketType GetPacketType(BinaryReader reader)
    {
        // check if can read a ushort (2 bytes) from the stream
        if (reader.BaseStream.Length - reader.BaseStream.Position < 2)
            return PacketType.Unknown;
        
        // get the packet type
        var packetType = (PacketType)reader.ReadUInt16();
        // check if the packet type is valid
        // if not, return PacketType.Unknown
        if (!Enum.IsDefined(typeof(PacketType), packetType))
            return PacketType.Unknown;
        
        // return the packet type
        return packetType;
    }
}