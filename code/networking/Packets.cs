using Godot;
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

    public static byte[] Serialise<T>(T data) where T : struct
    {
        var result = new byte[Marshal.SizeOf(typeof(T))];
        var pResult = GCHandle.Alloc(result, GCHandleType.Pinned);
        Marshal.StructureToPtr(data, pResult.AddrOfPinnedObject(), true);
        pResult.Free();
        return result;
    }

    public static T Deserialise<T>(this byte[] data) where T : struct
    {
        var pData = GCHandle.Alloc(data, GCHandleType.Pinned);
        var result = (T)Marshal.PtrToStructure(pData.AddrOfPinnedObject(), typeof(T));
        pData.Free();
        return result;
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
                return Marshal.SizeOf(new SubmarinePacket());
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
    public long sent;

    public HeaderPacket(int init_bodyID)
    {
        bodyID = init_bodyID;
        sent = DateTime.UtcNow.Ticks;
    }
}

public struct SyncPacket
{
    public long sync;

    public SyncPacket(long init_sync)
    {
        sync = init_sync;
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

public struct SubmarinePacket
{
    public int clientID;
    public float x, y;

    // CAR PHYSICS
    public float direction;
    public float steer;
    public float speed;
    public float structure; // What would this be?

    public SubmarinePacket(int init_clientID, float init_x, float init_y, float init_direction, float init_steer)
    {
        clientID = init_clientID;
        x = init_x;
        y = init_y;

        direction = init_direction;
        steer = init_steer;
        speed = 300.0f;
        structure = 30.0f;
    }
}