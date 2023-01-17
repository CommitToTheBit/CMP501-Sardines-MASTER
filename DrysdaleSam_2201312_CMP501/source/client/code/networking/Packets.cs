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

    /* ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- */
    /* This enclosed section is from: Stack Overflow (2017) Convert Object To Byte Array Without Serialization nor Padding. Available at https://stackoverflow.com/questions/47649798/convert-object-to-byte-array-without-serialization-nor-padding (Accessed: 17 January 2023) */
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
    /* ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- */

    public static int GetSize(int packetID)
    {
        // Header keys:
        // - 1XXX: 'Universal' join/leave packets
        // - 2XXX: Lobby packets
        // - 3XXX: Game packets
        // - 40XX: Diplomat-related packets
        // - 41XX: Captain-related packets
        switch (packetID)
        {
            case 1000: // Client Time Sync
                return Marshal.SizeOf(new SyncPacket());
            case 1001: // Client Self-ID
                return Marshal.SizeOf(new IDPacket());
            case 1002: // (NPC) Client Connected
                return Marshal.SizeOf(new IDPacket());
            case 1003: // (NPC) Client Disconnected
                return Marshal.SizeOf(new IDPacket());
            case 1200: // Server tells client to prepare for lobby...
                return Marshal.SizeOf(new EmptyPacket());    
            case 1201: // Server has match ready for joining client
                return Marshal.SizeOf(new EmptyPacket());
            case 2300: // Host starts game: match
                return Marshal.SizeOf(new EmptyPacket());
            case 2301: // Server has match ready for client in lobby
                return Marshal.SizeOf(new EmptyPacket());
            case 2310: // Host starts game: sandbox
                return Marshal.SizeOf(new EmptyPacket());
            case 2311: // Server has sandbox ready for client in lobby
                return Marshal.SizeOf(new EmptyPacket());
            case 3200: // Finished game switches back to lobby
                return Marshal.SizeOf(new EmptyPacket());
            case 3201: // Server has lobby ready for client in match/sandbox
                return Marshal.SizeOf(new EmptyPacket());
            case 4000: // Diplomat ID
                return Marshal.SizeOf(new RolePacket());
            case 4100: // Captain ID
                return Marshal.SizeOf(new SubmarinePacket());
            case 4101: // Submarine Position
                return Marshal.SizeOf(new PositionPacket());
            case 4102: // Soundwave Collision
                return Marshal.SizeOf(new MorsePacket());
            case 4190: // Captain sends audio...
                return Marshal.SizeOf(new AudioPacket());
            default:
                return Marshal.SizeOf(new EmptyPacket());
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

public struct EmptyPacket
{

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

    /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- */
    /* This enclosed section is from: Stack Overflow (2017) Safe Fixed Size Array in struct C#. Available at https://stackoverflow.com/questions/46279646/safe-fixed-size-array-in-struct-c-sharp (Accessed: 17 January 2023) */
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4*4)] // An IP address has at most 15 characters
    public char[] clientIP;
    /* ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- */

    public IDPacket(int init_clientID, char[] init_clientIP)
    {
        clientID = init_clientID;
        clientIP = init_clientIP; // CHECKME: What happens to arrays of size > 15?
    }
}

public struct RolePacket
{
    public int superpowerID;
    public int clientID;

    public RolePacket(int init_superpowerID, int init_clientID)
    {
        superpowerID = init_superpowerID;
        clientID = init_clientID;
    }
}

public struct SubmarinePacket
{
    public int clientID;
    public int superpowerID;
    public int submarineID;
    public bool nuclearCapability;

    public SubmarinePacket(int init_superpowerID, int init_submarineID, int init_clientID, bool init_nuclearCapability)
    {
        superpowerID = init_superpowerID;
        submarineID = init_submarineID;
        clientID = init_clientID;
        nuclearCapability = init_nuclearCapability;
    }
}

public struct PositionPacket
{
    public int submarineID;
    public float x, y;
    public float theta;
    public long timestamp; // Timestamp for when this position was true

    public PositionPacket(int init_submarineID, float init_x, float init_y, float init_theta, long init_timestamp)
    {
        submarineID = init_submarineID;

        x = init_x;
        y = init_y;
        theta = init_theta;
        timestamp = init_timestamp;
    }
}

public struct MorsePacket
{
    public int senderID;
    public int receiverID;
    public bool dot;
    public float range;
    public float angle;
    public long interval;

    public MorsePacket(int init_senderID, int init_receiverID, bool init_dot, float init_range, float init_angle, long init_interval)
    {
        senderID = init_senderID;
        receiverID = init_receiverID;

        dot = init_dot;
        range = init_range;
        angle = init_angle;
        interval = init_interval;
    }
}

public struct AudioPacket
{
    public int clientID;
    public float x, y;

    public AudioPacket(int init_clientID, float init_x, float init_y)
    {
        clientID = init_clientID;

        x = init_x;
        y = init_y;
    }
}