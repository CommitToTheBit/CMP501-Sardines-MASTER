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
                return Marshal.SizeOf(new HeaderPacket());
            case 1:
                return Marshal.SizeOf(new PositionPacket());
            default:
                return 0;
        }
    }
}

struct HeaderPacket
{
    public int packetID;

    public HeaderPacket(int init_packetID)
    {
        packetID = init_packetID;
    }
}

struct PositionPacket
{
    public int objectID;
    public float x, y;

    public PositionPacket(int init_objectID, float init_x, float init_y)
    {
        objectID = init_objectID;
        x = init_x;
        y = init_y;
    }
}