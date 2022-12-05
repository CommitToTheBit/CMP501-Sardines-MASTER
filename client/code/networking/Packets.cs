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

    /* --------------------------------------------------------------------------------------------------------------- */
    /* CC: https://stackoverflow.com/questions/47649798/convert-object-to-byte-array-without-serialization-nor-padding */
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
    /* --------------------------------------------------------------------------------------------------------------- */

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
            case 1002: // (NPC) Client ID
                return Marshal.SizeOf(new IDPacket());
            case 2300: // Host starts game: match
                return Marshal.SizeOf(new EmptyPacket());
            case 2301: // Server has intiialised match
                return Marshal.SizeOf(new EmptyPacket());
            case 2310: // Host starts game: sandbox
                return Marshal.SizeOf(new EmptyPacket());
            case 2311: // Server has initialised sandbox
                return Marshal.SizeOf(new EmptyPacket());
            case 3200: // Finished game switches back to lobby
                return Marshal.SizeOf(new EmptyPacket());
            case 3201: // Server has initialised lobby
                return Marshal.SizeOf(new EmptyPacket());
            case 4000: // Diplomat ID
                return Marshal.SizeOf(new RolePacket());
            case 4100: // Captain ID
                return Marshal.SizeOf(new RolePacket());
            case 4101: // Submarine Position
                return Marshal.SizeOf(new PositionPacket());
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

    /* ---------------------------------------------------------------------------------------- */
    /* CC: https://stackoverflow.com/questions/46279646/safe-fixed-size-array-in-struct-c-sharp */
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4*4)] // An IP address has at most 15 characters
    public char[] clientIP;
    /* ---------------------------------------------------------------------------------------- */

    public IDPacket(int init_clientID, char[] init_clientIP)
    {
        clientID = init_clientID;
        clientIP = init_clientIP; // CHECKME: What happens to arrays of size > 15?
    }
}

public struct RolePacket
{
    public int clientID;
    public int superpowerID;

    public RolePacket(int init_clientID, int init_superpowerID)
    {
        clientID = init_clientID;
        superpowerID = init_superpowerID;
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
