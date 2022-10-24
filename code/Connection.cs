using Godot;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.IO;

public class TCPConnection
{
    private const string CLIENTIP = "127.0.0.1";
    private const string SERVERIP = "127.0.0.1";
    private const int SERVERPORT = 5555;
    private const int MESSAGESIZE = 40;

    private Socket client;
    private byte[] buffer;

    private enum State { READ, WRITE, NULL };
    private State state;
    private int readCount;
    private int writeCount;

    public TCPConnection()
    {
        client = new Socket(IPAddress.Parse(CLIENTIP).AddressFamily,SocketType.Stream,ProtocolType.Tcp);
        buffer = new byte[MESSAGESIZE];
        state = State.NULL;
        readCount = 0;
        writeCount = 0;
    }

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

    public string GetBuffer()
    {
        return Encoding.UTF8.GetString(buffer).Split("#")[0];
    }

    public void SetBuffer(string message)
    {
        buffer = Encoding.UTF8.GetBytes(message.PadRight(MESSAGESIZE,'#'));
    }

    public bool ReadingToBuffer()
    {
        return state == State.READ;
    }

    public bool ReadToBuffer()
    {
        try
        {
            // Receive as much data from the client as will fit in the buffer.
            int bufferLeft = MESSAGESIZE - readCount;
            int count = client.Receive(buffer, readCount, bufferLeft, 0);

            if (count <= 0)
                throw new Exception();

            //We've successfully read some more data into the buffer...
            readCount += count;
        }
        catch
        {
            return true;
        }

        if (readCount < MESSAGESIZE)
        {
            // ... but we've not received a complete message yet.
            // So we can't do anything until we receive some more.
            state = State.READ;
            return false;
        }

        // We've got a complete message.
        state = State.NULL;
        readCount = 0;

        GD.Print("Received message from the client: "+Encoding.UTF8.GetString(buffer));

        return false;
    }

    public bool WritingFromBuffer()
    {
        return state == State.READ;
    }

    public bool WriteFromBuffer()
    {     
        try
        {
            // Receive as much data from the client as will fit in the buffer.
            int bufferLeft = MESSAGESIZE - writeCount;
            int count = client.Send(buffer, writeCount, bufferLeft, 0);

            if (count <= 0)
                throw new Exception();

            // We've successfully written some more data into the buffer.
            writeCount += count;
        }
        catch
        {
            return true;
        }

        if (writeCount < MESSAGESIZE)
        {
            // ... but we've not sent a complete message yet.
            // So we can't do anything until we receive some more.
            state = State.WRITE;
            return false;
        }

        // Clear the buffer, ready for the next message.
        state = State.NULL;
        writeCount = 0;

        return false;
    }

    public void QueueWriteFromBuffer()
    {
        state = State.WRITE;
    }

    public void SerialisePositionPacket()
    {
        PositionPacket packet = new PositionPacket(0,1.23f,0.0f);
        byte[] bytes = PacketSerialiser.Serialise<PositionPacket>(packet);
        GD.Print(bytes.Length);
        PositionPacket dPacket = PacketSerialiser.Deserialise<PositionPacket>(bytes);
        GD.Print(dPacket.x);
    }
}
