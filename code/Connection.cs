using Godot;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Runtime.InteropServices;

public class TCPConnection
{
    // Constants
    private const string CLIENTIP = "127.0.0.1";
    private const string SERVERIP = "127.0.0.1";
    private const int SERVERPORT = 5555;

    private static int HEADERSIZE = Marshal.SizeOf(new HeaderPacket());

    // Variables
    private Socket client;

    private enum ReadState { HEADER, PACKET, NULL };
    private ReadState readState;

    private enum WriteState { HEADER, PACKET, NULL };
    private WriteState writeState;

    private int packetID;
    private int packetSize;

    private byte[] readHeaderBuffer;
    private byte[] readPacketBuffer;
    private int readCount;

    private byte[] writeHeaderBuffer;
    private byte[] writePacketBuffer;
    private int writeCount;

    // Constructor
    public TCPConnection()
    {
        client = new Socket(IPAddress.Parse(CLIENTIP).AddressFamily,SocketType.Stream,ProtocolType.Tcp); 

        readState = ReadState.HEADER;
        writeState = WriteState.HEADER;

        packetID = -1;
        packetSize = Packet.GetSize(packetID);

        readHeaderBuffer = new byte[HEADERSIZE];
        readPacketBuffer = new byte[packetSize];
        readCount = 0;

        writeHeaderBuffer = new byte[HEADERSIZE];
        writePacketBuffer = new byte[packetSize];
        writeCount = 0;
    }

    // Destructor
    ~TCPConnection()
    {
        Console.WriteLine("Closing connection...");
        client.Close();
    }

    // Accessors
    public Socket GetSocket()
    {
        return client;
    }

    // Functions
    public bool Connect()
    {
        try
        {
            client.Connect(new IPEndPoint(IPAddress.Parse(SERVERIP),SERVERPORT));

            GD.Print("Connected to server\n");

            return true;
        }
        catch
        {
            GD.Print("Waiting to connect...");

            return false;
        }
    }

    public bool IsRead()
    {
        return readState != ReadState.NULL && writeState == WriteState.NULL;
    }

    public bool Read()
    {
        if (readState == ReadState.HEADER)
            return ReadHeader();
        else if (readState == ReadState.PACKET)
            return ReadPacket();

        Console.WriteLine("Error in readState");
        return true;
    }

    private bool ReadHeader()
    {
        // Read into buffer
        try
        {
            int bufferLeft = HEADERSIZE - readCount;
            int count = client.Receive(readHeaderBuffer, readCount, bufferLeft, 0);

            if (count <= 0)
                throw new Exception();

            readCount += count;
        }
        catch
        {
            Console.WriteLine("Client connection closed or broken");
            return true;
        }

        // Wait for buffer to fill up
        if (readCount < HEADERSIZE)
            return false;

        // Process contents of header
        HeaderPacket header = Packet.Deserialise<HeaderPacket>(readHeaderBuffer);
        packetID = header.packetID;
        packetSize = Packet.GetSize(packetID);

        // Prepare to read packet
        readState = ReadState.PACKET;
        readCount = 0;

        return false;
    }

    private bool ReadPacket()
    {
        // Read into buffer
        try
        {
            int bufferLeft = packetSize - readCount;
            int count = client.Receive(readPacketBuffer, readCount, bufferLeft, 0);

            if (count <= 0)
                throw new Exception();

            readCount += count;
        }
        catch
        {
            Console.WriteLine("Client connection closed or broken");
            return true;
        }

        // Wait for buffer to fill up
        if (readCount < packetSize)
            return false;

        // Process contents of packet
        if (packetID == 1)
        {
            PositionPacket position = Packet.Deserialise<PositionPacket>(readPacketBuffer);
            Console.WriteLine("Object " + position.objectID.ToString() + " has position (" + position.x.ToString() + ", " + position.y.ToString() + ")");
        }

        // Reset for next header
        packetID = -1;
        packetSize = Packet.GetSize(packetID);
        readHeaderBuffer = new byte[HEADERSIZE];
        readPacketBuffer = new byte[packetSize];

        // Prepare to read header
        readState = ReadState.HEADER;
        readCount = 0;

        return false;
    }
}
