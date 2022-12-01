using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Runtime.InteropServices;

public static class Packet
{
    static Packet()
    {

    }

    public static byte[] Serialise<T>(T packet) where T : struct
    {
        byte[] data = new byte[Marshal.SizeOf(typeof(T))];

        GCHandle pData = GCHandle.Alloc(data, GCHandleType.Pinned);
        Marshal.StructureToPtr(packet, pData.AddrOfPinnedObject(), true);
        pData.Free();

        return data;
    }

    public static T Deserialise<T>(this byte[] data) where T : struct
    {
        GCHandle pData = GCHandle.Alloc(data, GCHandleType.Pinned);
        T packet = (T)Marshal.PtrToStructure(pData.AddrOfPinnedObject(), typeof(T));
        pData.Free();

        return packet;
    }

    public static int GetSize(int packetID)
    {
        switch (packetID)
        {
            case 0:
                return Marshal.SizeOf(new SyncPacket());
            case 1:
                return Marshal.SizeOf(new IDPacket());
            case 2:
                return Marshal.SizeOf(new PositionPacket());
            default:
                return 0;
        }
    }
}

public struct SendablePacket
{
    public HeaderPacket header;
    public byte[] serialisedBody;

    public SendablePacket(HeaderPacket init_header, byte[] init_serialisedBody)
    {
        header = init_header;
        serialisedBody = init_serialisedBody;
    }
}

public struct HeaderPacket
{
    public int bodyID;
    public long timestamp;

    public HeaderPacket(int init_bodyID)
    {
        bodyID = init_bodyID;
        timestamp = DateTime.UtcNow.Ticks;
    }
}

public struct SyncPacket
{
    public long syncTimestamp;

    public SyncPacket(long init_syncTimestamp)
    {
        syncTimestamp = init_syncTimestamp;
    }
}


public struct IDPacket
{
    public int clientID;

    public IDPacket(int init_clientID)
    {
        clientID = init_clientID;
    }
}

public struct PositionPacket
{
    public int clientID; // clientID of submarine
    public float x, y;
    public float theta;
    public long timestamp; // Timestamp for when this position was true

    public PositionPacket(int init_clientID, float init_x, float init_y, float init_theta, long init_timestamp)
    {
        clientID = init_clientID;

        x = init_x;
        y = init_y;
        theta = init_theta;
        timestamp = init_timestamp;
    }
}
