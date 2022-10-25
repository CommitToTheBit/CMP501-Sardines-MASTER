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

    private enum State { READ, WRITE };
    private State state;

    private int readBodyID;
    private int readBodySize;
    private byte[] readHeaderBuffer;
    private byte[] readBodyBuffer;
    private int readCount;

    private int writeBodyID;
    private int writeBodySize;
    private byte[] writeHeaderBuffer;
    private byte[] writeBodyBuffer;
    private int writeCount;

    // Constructor
    public TCPConnection()
    {
        client = new Socket(IPAddress.Parse(CLIENTIP).AddressFamily,SocketType.Stream,ProtocolType.Tcp); 

        State state = State.READ;

        readBodyID = -1;
        readBodySize = Packet.GetSize(readBodyID);
        readHeaderBuffer = new byte[HEADERSIZE];
        readBodyBuffer = new byte[readBodySize];
        readCount = 0;

        writeBodyID = -1;
        writeBodySize = Packet.GetSize(writeBodyID);
        writeHeaderBuffer = new byte[HEADERSIZE];
        writeBodyBuffer = new byte[writeBodySize];
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
        return state == State.READ;
    }

    public bool Read()
    {
        // Read into header buffer
        if (readCount < HEADERSIZE)
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

        // Wait for header buffer to fill up
        if (readCount < HEADERSIZE)
            return false;

        // Interpret header buffer
        HeaderPacket header = Packet.Deserialise<HeaderPacket>(readHeaderBuffer);
        readBodyID = header.bodyID;
        readBodySize = Packet.GetSize(readBodyID);

        // Read into body buffer
        if (readCount < HEADERSIZE+readBodySize)
            try
            {
                int bufferLeft = HEADERSIZE + readBodySize - readCount;
                int count = client.Receive(readBodyBuffer, readCount, bufferLeft, 0);

                if (count <= 0)
                    throw new Exception();

                readCount += count;
            }
            catch
            {
                Console.WriteLine("Client connection closed or broken");
                return true;
            }

        // Wait for body buffer to fill up
        if (readCount < HEADERSIZE+readBodySize)
            return false;

        // Process body buffer
        if (readBodyID == 1)
        {
            PositionPacket positionPacket = Packet.Deserialise<PositionPacket>(readBodyBuffer);
            Console.WriteLine(positionPacket.x);
        }

        // Prepare to write packet
        state = State.WRITE;
        readCount = 0;

        return false;
    }

    public bool IsWrite()
    {
        return state == State.WRITE;
    }

    public bool Write()
    {
        // Write out of header buffer
        if (writeCount < HEADERSIZE)
            try
            {
                int bufferLeft = HEADERSIZE - writeCount;
                int count = client.Send(writeHeaderBuffer, writeCount, bufferLeft, 0);

                if (count <= 0)
                    throw new Exception();

                writeCount += count;
            }
            catch
            {
                Console.WriteLine("Client connection closed or broken");
                return true;
            }

        // Wait for header buffer to fully send
        if (writeCount < HEADERSIZE)
            return false;

        // Write out of body buffer
        if (writeCount < HEADERSIZE+writeBodySize)
            try
            {
                int bufferLeft = writeBodySize-(writeCount-HEADERSIZE);
                int count = client.Send(writeBodyBuffer, 0, bufferLeft, 0);

                if (count <= 0)
                    throw new Exception();

                writeCount += count;
            }
            catch
            {
                Console.WriteLine("Client connection closed or broken");
                return true;
            }

        // Wait for body buffer to fully send
        if (writeCount < HEADERSIZE)
            return false;

        // Prepare to read packet
        state = State.READ;
        writeCount = 0;

        GD.Print("all sent!");

        return false;
    }

    public void Send(SendablePacket packet)
    {
        writeBodyID = packet.header.bodyID;
        writeBodySize = Packet.GetSize(writeBodyID);
        writeHeaderBuffer = Packet.Serialise<HeaderPacket>(packet.header);
        writeBodyBuffer = packet.serialisedBody;

        Write();
        Read();
    }
}
