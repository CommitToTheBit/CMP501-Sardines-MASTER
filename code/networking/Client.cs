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

    private long started;

    // Constructor
    public Client()
    {
        clientSocket = new Socket(IPAddress.Parse(CLIENTIP).AddressFamily,SocketType.Stream,ProtocolType.Tcp); 

        serverConnection = new TCPConnection(clientSocket);
        disconnected = true;

        // Initialise for New Game 
        clientID = -1;
        state = new State();

        started = DateTime.UtcNow.Ticks;
        Console.WriteLine("Console started at " + started+".");
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
            disconnected = false;

            SendIDPacket();
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
    private void SendSyncPacket(long syncStarted)
    {
        /*
        *
        */
        HeaderPacket header = new HeaderPacket(0);
        SyncPacket sync = new SyncPacket(syncStarted);
        SendablePacket packet = new SendablePacket(header,Packet.Serialise<SyncPacket>(sync));
        serverConnection.SendPacket(packet);
    }

    private void SendIDPacket()
    {
        /*
        *   Identify self to server on connection
        */
        HeaderPacket header = new HeaderPacket(1);
        IDPacket id = new IDPacket(clientID);
        SendablePacket packet = new SendablePacket(header,Packet.Serialise<IDPacket>(id));
        serverConnection.SendPacket(packet);
    }

    public void SendSubmarinePacket(int clientID, float gas, float brakes, float steer, float a, float u, float x, float y, float theta)
    {
        /*
        *   Send details of own submarine to server
        */
        // FIXME: Since this will never be sent erroneously, can't we remove all arguments?
        HeaderPacket header = new HeaderPacket(2);
        SubmarinePacket submarine = new SubmarinePacket(clientID,gas,brakes,steer,a,u,x,y,theta,DateTime.Now.Ticks);
        SendablePacket packet = new SendablePacket(header,Packet.Serialise<SubmarinePacket>(submarine));
        serverConnection.SendPacket(packet);
    }

    // Receive functions
    private void ReceivePacket(SendablePacket packet) // Redirects each type of packet
    {
        switch (packet.header.bodyID)
        {
            case 0:
                SyncPacket syncPacket = Packet.Deserialise<SyncPacket>(packet.serialisedBody);
                ReceiveSyncPacket(syncPacket,packet.header.sent);
                break;
            case 1:
                IDPacket idPacket = Packet.Deserialise<IDPacket>(packet.serialisedBody);
                ReceiveIDPacket(idPacket);
                break;
            case 2:
                SubmarinePacket submarinePacket = Packet.Deserialise<SubmarinePacket>(packet.serialisedBody);
                ReceiveSubmarinePacket(submarinePacket);
                break;    
        }
    }

    private void ReceiveSyncPacket(SyncPacket packet, long syncStarted)
    {
        /*
        *   The server has sent its 'official' start time
        */
        started = packet.sync;
        Console.WriteLine("Server started at " + started+".");
        SendSyncPacket(syncStarted);
    }

    private void ReceiveIDPacket(IDPacket packet)
    {
        /*
        *   If clientID is null (-1), assign a new ID.
        */
        if (clientID < 0)
            clientID = packet.clientID;
    }

    private void ReceiveSubmarinePacket(SubmarinePacket packet)
    {
        /*
        *   Update nearby submarines, and forget about submarines out of range. 
        */
        if (packet.clientID < 0)
            return;

        // FIXME: Add range checks on server and client sides...
        // FIXME: Who should get priority if client and server update the submarine *at the same time*?
        state.UpdateSubmarine(packet.clientID,packet.gas,packet.brakes,packet.steer,packet.a,packet.u,packet.x,packet.y,packet.theta,packet.t0);
    }

    // Accessors
    public int GetClientID()
    {
        return clientID;
    }
}

