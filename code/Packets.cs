using Godot;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

using System.Runtime.InteropServices;

public static class PacketSerialiser
{
    static PacketSerialiser()
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
}

struct PositionPacket
{
    public Int64 objectID;
    public float x, y;

    public PositionPacket(int init_objectID, float init_x, float init_y)
    {
        objectID = init_objectID;
        x = init_x;
        y = init_y; 
    }
}