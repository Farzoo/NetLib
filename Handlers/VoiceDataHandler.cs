using System.Numerics;
using NAudio.Wave;
using NetLib.Packets.Shared;
using NetLib.Server;

namespace NetLib.Handlers;

public class VoiceDataHandler : IPacketReceivedHandler
{
    public void OnPacketReceived(BaseClient baseClient, PacketBase packet)
    {
        if(packet is VoiceDataPacket voiceDataPacket)
        {
            WaveOut waveOut = new WaveOut();
            waveOut.Init(new RawSourceWaveStream(voiceDataPacket.Data, 0, voiceDataPacket.Data.Length, new WaveFormat(44100, 1)));
            waveOut.Play();
        }
    }

    public ushort PacketId => (ushort) PacketType.VoiceData;
}