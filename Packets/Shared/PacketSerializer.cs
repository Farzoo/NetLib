using System.ComponentModel;
using System.Reflection;
using Microsoft.IO;
using ProtoBuf;
using ProtoBuf.Meta;

namespace NetLib.Packets.Shared;

public interface IPacketSerializer<T> where T : PacketBase
{
    byte[] Serialize(T packet);
    T Deserialize(byte[] data);
    
    ushort GetPacketId(byte[] data);
}

public class PacketSerializer<T>
    : IPacketSerializer<T>
    where T : PacketBase
{
    public PacketSerializer()
    {
        this.CheckType();
        this.RegisterTypeForBaseType();
    }
    
    private void CheckType()
    {
        bool hasParameterlessConstructor = typeof(T).GetConstructors().Any(c => c.GetParameters().Length == 0);
        if(!hasParameterlessConstructor)
            throw new Exception("Packet type must have a parameterless constructor");
    }
    private ushort RegisterTypeForBaseType()
    {
        PacketInfo? packetInfo = typeof(T).GetCustomAttribute<PacketInfo>(false);
        if (packetInfo == null)
            throw new Exception("Packet must have PacketInfo attribute");
        MetaType thisType = RuntimeTypeModel.Default[typeof(T)];
        MetaType baseType = RuntimeTypeModel.Default[typeof(T).BaseType];
        if (baseType.GetSubtypes().All(s => s.DerivedType != thisType))
        {
            if(baseType.GetSubtypes().Any(s => s.FieldNumber == packetInfo.Id))
                throw new Exception("Packet id already registered for another packet");
            baseType.AddSubType(packetInfo.Id + 1, typeof(T));
        }
        return packetInfo.Id;
    }
    public byte[] Serialize(T packet)
    {
        using MemoryStream stream = new MemoryStream();
        stream.Write(BitConverter.GetBytes(packet.Id), 0, sizeof(ushort));
        ProtoBuf.Serializer.Serialize(stream, packet);
        return stream.ToArray();
    }

    public T Deserialize(byte[] data)
    {
        using MemoryStream stream = new MemoryStream(data);
        stream.Seek(sizeof(ushort), SeekOrigin.Begin);
        return ProtoBuf.Serializer.Deserialize<T>(stream);
    }

    public ushort GetPacketId(byte[] data)
    {
        using MemoryStream stream = new MemoryStream(data);
        using BinaryReader reader = new BinaryReader(stream);
        return reader.ReadUInt16();
    }
}

public interface IPacketSerializer
{
    byte[] Serialize(PacketBase packet);
    PacketBase Deserialize(byte[] data);
}

public class PacketSerializer : IPacketSerializer
{
    private IPacketMapper Mapper { get; }

    public PacketSerializer(IPacketMapper mapper)
    {
        this.Mapper = mapper;
        this.Mapper.GetTypes().ForEach(t =>
        {
            this.CheckType(t);
            this.RegisterTypeForBaseType(t);
        });
    }

    private void CheckType(Type type)
    {
        bool hasParameterlessConstructor = type.GetConstructors().Any(c => c.GetParameters().Length == 0);
        if(!hasParameterlessConstructor)
            throw new Exception("Packet type must have a parameterless constructor");
    }

    private ushort RegisterTypeForBaseType(Type type)
    {
        if(type.BaseType == null || type.BaseType != typeof(PacketBase))
            throw new Exception($"Cannot register {type.Name} for serializer. It must inherit from PacketBase");
        PacketInfo? packetInfo = type.GetCustomAttribute<PacketInfo>(false);
        if (packetInfo == null)
            throw new Exception($"{type.Name} must have a PacketInfo");
        MetaType thisType = RuntimeTypeModel.Default[type];
        MetaType baseType = RuntimeTypeModel.Default[type.BaseType];
        if (baseType.GetSubtypes().All(s => s.DerivedType != thisType))
        {
            if(baseType.GetSubtypes().Any(s => s.FieldNumber == packetInfo.Id))
                throw new Exception("Packet id already registered for another packet");
            baseType.AddSubType(packetInfo.Id + 1, type);
        }
        return packetInfo.Id;
    }

    public PacketBase Deserialize(byte[] data)
    {
        using MemoryStream stream = new MemoryStream(data);
        using BinaryReader reader = new BinaryReader(stream);
        ushort length = reader.ReadUInt16();
        ushort id = reader.ReadUInt16();
        Type? type = this.Mapper.GetType(id);
        if (type == null) return new UnknownPacket();
        reader.BaseStream.SetLength(length);
        reader.BaseStream.Seek(sizeof(ushort)*2, SeekOrigin.Begin);
        return ProtoBuf.Serializer.NonGeneric.Deserialize(type, stream) as PacketBase ?? new UnknownPacket();
    }

    public byte[] Serialize(PacketBase packet)
    {
        using MemoryStream stream = new MemoryStream();
        using BinaryWriter writer = new BinaryWriter(stream);
        writer.Seek(sizeof(ushort), SeekOrigin.Begin);
        writer.Write(packet.Id);
        ProtoBuf.Serializer.Serialize(writer.BaseStream, packet);
        writer.Seek(0, SeekOrigin.Begin);
        writer.Write((ushort)writer.BaseStream.Length);
        return stream.ToArray();
    }
}