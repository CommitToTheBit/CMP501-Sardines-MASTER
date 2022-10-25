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

    private int clientID;
    public State state;

    // Constructor
    public Client()
    {
        clientSocket = new Socket(IPAddress.Parse(CLIENTIP).AddressFamily,SocketType.Stream,ProtocolType.Tcp); 

        serverConnection = new TCPConnection(clientSocket);
        disconnected = true;

        clientID = -1;
        state = new State();
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

            SendIDPacket();
            SendSubmarinePacket(0.0f,0.0f);
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
            ReceivePacket(packet);

            GD.Print("Received a packet with bodyID "+packet.header.bodyID.ToString());
            GD.Print("Client ID is "+clientID);
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

    // Send functions
    private void SendIDPacket()
    {
        HeaderPacket header = new HeaderPacket(0);
        IDPacket id = new IDPacket(clientID);
        SendablePacket packet = new SendablePacket(header,Packet.Serialise<IDPacket>(id));
        serverConnection.SendPacket(packet);
    }

    public void SendSubmarinePacket(float x, float y)
    {
        HeaderPacket header = new HeaderPacket(1);
        SubmarinePacket submarine = new SubmarinePacket(clientID,x,y);
        SendablePacket packet = new SendablePacket(header,Packet.Serialise<SubmarinePacket>(submarine));
        serverConnection.SendPacket(packet);
    }

    // Receive functions
    private void ReceivePacket(SendablePacket packet)
    {
        switch (packet.header.bodyID)
        {
            case 0:
                IDPacket idPacket = Packet.Deserialise<IDPacket>(packet.serialisedBody);
                ReceiveIDPacket(idPacket);
                break;
            case 1:
                SubmarinePacket submarinePacket = Packet.Deserialise<SubmarinePacket>(packet.serialisedBody);
                ReceiveSubmarinePacket(submarinePacket);
                break;    
        }
    }

    private void ReceiveIDPacket(IDPacket packet)
    {
        if (clientID < 0)
            clientID = packet.clientID;
    }

    private void ReceiveSubmarinePacket(SubmarinePacket packet)
    {
        state.UpdateSubmarine(packet.clientID,packet.x,packet.y);
    }

    //
    public int GetClientID()
    {
        return clientID;
    }
}

