using System.Collections.Concurrent;
using NAudio.Wave;
using NetLib.Packets.Shared;
using NetLib.Server;

namespace NetLib.Handlers.Client;

public class ClientsVoiceHandler : IPacketReceivedHandler
{
    private IDictionary<Guid, ClientVoiceOut> Clients { get; } = new ConcurrentDictionary<Guid, ClientVoiceOut>();
    private WaveFormat DefaultFormat { get; } 
    private int DefaultLatency { get; }
    private int DefaultDurationMultiplier { get; }

    public ClientsVoiceHandler(WaveFormat defaultFormat, int defaultLatency, int defaultDurationMultiplier)
    {
        this.DefaultFormat = defaultFormat;
        this.DefaultLatency = defaultLatency;
        this.DefaultDurationMultiplier = defaultDurationMultiplier;
    }
    
    public void OnPacketReceived(BaseClient baseClient, PacketBase packet)
    {
        if(packet is not VoiceDataPacket voicePacket) return;
        
        lock(this.Clients)
        {
            if (!this.Clients.ContainsKey(voicePacket.EntityId))
            {
                this.ConnectClient(voicePacket.EntityId);
            }
        }
        
        this.Clients[voicePacket.EntityId].PlayReceivedVoice(voicePacket);
    }
    
    public void DisconnectClient(Guid clientId)
    {
        lock(this.Clients)
        {
            if (this.Clients.ContainsKey(clientId))
            {
                this.Clients[clientId].Dispose();
                this.Clients.Remove(clientId);
            }
        }
    }
    
    public void ConnectClient(Guid clientId)
    {
        lock(this.Clients)
        {
            this.Clients.Add
            (
                clientId,
                new ClientVoiceOut(clientId, this.DefaultFormat, this.DefaultLatency, this.DefaultDurationMultiplier)
            );
        }
    }

    public ushort PacketId => (ushort) PacketType.VoiceData;
}