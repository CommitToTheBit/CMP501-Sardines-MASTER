using Godot;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Runtime.InteropServices;
using System.Collections.Generic;

public class Client
{
    private const string CLIENTIP = "127.0.0.1";
    private const string SERVERIP = "127.0.0.1";
    private const int SERVERPORT = 5555;

    // Variables
    private Socket clientSocket;

    private TCPConnection serverConnection;
    private bool disconnected;

    // Constructor
    public Client()
    {
        clientSocket = new Socket(IPAddress.Parse(CLIENTIP).AddressFamily,SocketType.Stream,ProtocolType.Tcp); 

        serverConnection = new TCPConnection(clientSocket);
        disconnected = true;
    }

    // Destructor
    ~Client()
    {
        clientSocket.Close();
    }
    
    // Accessors
    public bool IsConnected()
    {
        return !disconnected;
    }

    // Public functions
    public void Connect()
    {
        try
        {
            clientSocket.Connect(new IPEndPoint(IPAddress.Parse(SERVERIP),SERVERPORT));

            GD.Print("Connected to server\n");

            disconnected = false;
        }
        catch
        {
            disconnected = true;
        }
    }

    public void Read()
    {
        // The structure that describes the set of sockets we're interested in reading from.
        List<Socket> readable = new List<Socket>();

        // The structure that describes the set of sockets we're interested in writing to.
        List<Socket> writeable = new List<Socket>();

        if (serverConnection.IsRead())
        {
            readable.Add(serverConnection.GetSocket());
        }
        if (serverConnection.IsWrite())
        {
            writeable.Add(serverConnection.GetSocket());
        }

        Socket.Select(readable, writeable, null, 0);

        if (readable.Contains(serverConnection.GetSocket()))
            disconnected |= serverConnection.Read();
    }

    public void Update()
    {
        while (serverConnection.isRecvPacket())
        {
            SendablePacket packet = serverConnection.RecvPacket();
            GD.Print("Received a packet with bodyID "+packet.header.bodyID.ToString());
            if (packet.header.bodyID == 1)
            {
                PositionPacket position = Packet.Deserialise<PositionPacket>(packet.serialisedBody);
                Console.WriteLine("This is a PositionPacket saying object "+position.objectID.ToString()+" has coordinates ("+position.x.ToString()+", "+position.y.ToString()+")");
                //sprite.Position = new Vector2(position.x,position.y);
            }
        }
    }

    public void Write()
    {
        // The structure that describes the set of sockets we're interested in reading from.
        List<Socket> readable = new List<Socket>();

        // The structure that describes the set of sockets we're interested in writing to.
        List<Socket> writeable = new List<Socket>();

        if (serverConnection.IsRead())
        {
            readable.Add(serverConnection.GetSocket());
        }
        if (serverConnection.IsWrite())
        {
            writeable.Add(serverConnection.GetSocket());
        }

        Socket.Select(readable, writeable, null, 0);

        if (writeable.Contains(serverConnection.GetSocket()))
            disconnected |= serverConnection.Write();
    }

    public void SendPacket(SendablePacket packet)
    {
        serverConnection.SendPacket(packet);
    }

    // Private functions
    private void ProcessPacket(SendablePacket packet)
    {
        //switch (packet.header.bodyID)
    }
}

