using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Runtime.InteropServices;
using System.Collections.Generic;

public class TCPConnection
{
    // Constants
    private static int HEADERSIZE = Marshal.SizeOf(new HeaderPacket());

    // Variables
    private Socket socket;

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

    private List<SendablePacket> recvQueue;
    private List<SendablePacket> sendQueue;

    public bool disconnect;

    // FIXME: Add delays to connections?

    // Constructor
    public TCPConnection(Socket init_socket)
    {
        socket = init_socket;

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

        recvQueue = new List<SendablePacket>();
        sendQueue = new List<SendablePacket>();

        disconnect = false;

        // FIXME: Add delays to connections!
        //delay = 0;
    }

    // Destructor
    ~TCPConnection()
    {
        socket.Close();
    }

    // Accessors
    public Socket GetSocket()
    {
        return socket;
    }

    // Functions
    public bool IsRead()
    {
        return sendQueue.Count == 0;
    }

    public bool Read()
    {
        // Read into header buffer
        if (readCount < HEADERSIZE)
            try
            {
                int bufferLeft = HEADERSIZE - readCount;
                int count = socket.Receive(readHeaderBuffer, readCount, bufferLeft, 0);

                if (count <= 0)
                    throw new Exception();

                readCount += count;
            }
            catch
            {
                return true;
            }

        // Wait for header buffer to fill up
        if (readCount < HEADERSIZE)
            return false;

        // Interpret header buffer
        HeaderPacket header = Packet.Deserialise<HeaderPacket>(readHeaderBuffer);
        readBodyID = header.bodyID;
        readBodySize = Packet.GetSize(readBodyID);
        readBodyBuffer = new byte[readBodySize];

        // Read into body buffer
        if (readCount < HEADERSIZE+readBodySize)
            try
            {
                int bufferLeft = readBodySize - (readCount - HEADERSIZE);
                int count = socket.Receive(readBodyBuffer, readCount-HEADERSIZE, bufferLeft, 0);

                if (count <= 0)
                    throw new Exception();

                readCount += count;
            }
            catch
            {
                return true;
            }

        // Wait for body buffer to fill up
        if (readCount < HEADERSIZE+readBodySize)
            return false;

        // Reset reading
        recvQueue.Add(new SendablePacket(header, readBodyBuffer));

        readBodyID = -1;
        readBodySize = Packet.GetSize(readBodyID);
        readHeaderBuffer = new byte[HEADERSIZE];
        readBodyBuffer = new byte[readBodySize];
        readCount = 0;

        return false;
    }

    public bool IsWrite()
    {
        return sendQueue.Count > 0;
    }

    public bool Write()
    {
        // Check we have a message worth sending
        if (sendQueue.Count == 0)
            return false;

        writeBodyID = sendQueue[0].header.bodyID;
        writeBodySize = Packet.GetSize(writeBodyID);
        writeHeaderBuffer = Packet.Serialise<HeaderPacket>(sendQueue[0].header);
        writeBodyBuffer = sendQueue[0].serialisedBody;

        // Write out of header buffer
        if (writeCount < HEADERSIZE)
            try
            {
                int bufferLeft = HEADERSIZE - writeCount;
                int count = socket.Send(writeHeaderBuffer, writeCount, bufferLeft, 0);

                if (count <= 0)
                    throw new Exception();

                writeCount += count;
            }
            catch
            {
                return true;
            }

        // Wait for header buffer to fully send
        if (writeCount < HEADERSIZE)
            return false;

        // Write out of body buffer
        if (writeCount < HEADERSIZE+writeBodySize)
            try
            {
                int bufferLeft = writeBodySize - (writeCount - HEADERSIZE);
                int count = socket.Send(writeBodyBuffer, writeCount - HEADERSIZE, bufferLeft, 0);

                if (count <= 0)
                    throw new Exception();

                writeCount += count;
            }
            catch
            {
                return true;
            }

        // Wait for body buffer to fully send
        if (writeCount < HEADERSIZE)
            return false;

        // Reset writing
        sendQueue.RemoveAt(0);

        writeBodyID = -1;
        writeBodySize = Packet.GetSize(writeBodyID);
        writeHeaderBuffer = new byte[HEADERSIZE];
        writeBodyBuffer = new byte[writeBodySize];
        writeCount = 0;

        return false;
    }

    public bool isRecvPacket()
    {
        return recvQueue.Count > 0;
    }

    public SendablePacket RecvPacket()
    {
        SendablePacket packet = recvQueue[0];
        recvQueue.RemoveAt(0);
        return packet;
    }

    public void SendPacket(SendablePacket packet)
    {
        sendQueue.Add(packet);
    }
}
