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
    private const string SERVERIP = "192.168.1.200";
    private const int SERVERPORT = 5555;

    // Variables
    private Socket clientSocket;
    private TCPConnection serverConnection;
    private bool disconnected;

    private int clientID;
    public State state;

    private long started;

    public long delay;

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
        Console.WriteLine("Client started at " + started+".");
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

            SendSyncPacket();
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
    private void SendSyncPacket()
    {
        /*
        *
        */
        HeaderPacket header = new HeaderPacket(0);
        SyncPacket sync = new SyncPacket(0);
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

    public void SendPositionPacket(float x, float y, float theta, long timestamp)
    {
        /*
        *   Send details of own submarine to server
        */
        // FIXME: Since this will never be sent erroneously, can't we remove all arguments?
        HeaderPacket header = new HeaderPacket(2);
        PositionPacket submarine = new PositionPacket(clientID,x,y,theta,timestamp);
        SendablePacket packet = new SendablePacket(header,Packet.Serialise<PositionPacket>(submarine));
        serverConnection.SendPacket(packet);
    }

    // Receive functions
    private void ReceivePacket(SendablePacket packet) // Redirects each type of packet
    {
        switch (packet.header.bodyID)
        {
            case 0:
                SyncPacket syncPacket = Packet.Deserialise<SyncPacket>(packet.serialisedBody);
                ReceiveSyncPacket(syncPacket,packet.header.timestamp);
                break;
            case 1:
                IDPacket idPacket = Packet.Deserialise<IDPacket>(packet.serialisedBody);
                ReceiveIDPacket(idPacket);
                break;
            case 2:
                PositionPacket positionPacket = Packet.Deserialise<PositionPacket>(packet.serialisedBody);
                ReceivePositionPacket(positionPacket);
                break;    
        }
    }

    private void ReceiveSyncPacket(SyncPacket packet, long serverMoment)
    {
        /*
        *   The server has sent its 'official' start time
        */

        // clientMoment and serverMoment should occur at (roughly) the same time
        long clientMoment = (packet.syncTimestamp+DateTime.UtcNow.Ticks)/2;

        // If clientMoment < serverMoment, the client is delayed behind the server
        // We add this delay to future client calculations, to 'catch them up' with the server
        delay = serverMoment-clientMoment;

        // DEBUG:
        Console.WriteLine("We are a delay of "+delay+" behind the server...");

        // Now that we've (re-)synced, we send our initial ID Packet...
        SendIDPacket();
    }

    private void ReceiveIDPacket(IDPacket packet)
    {
        /*
        *   If clientID is null (-1), assign a new ID.
        */
        if (clientID < 0)
            clientID = packet.clientID;
    }

    private void ReceivePositionPacket(PositionPacket packet)
    {
        /*
        *   Update nearby submarines, and forget about submarines out of range. 
        */
        Console.WriteLine("Client "+clientID+": Received position packet about Client "+packet.clientID+"...");

        if (packet.clientID < 0)
            return;

        // FIXME: Add range checks on server and client sides...
        // FIXME: Who should get priority if client and server update the submarine *at the same time*?
        state.UpdateSubmarine(packet.clientID,packet.x,packet.y,packet.theta,packet.timestamp);
    }

    // Accessors
    public int GetClientID()
    {
        return clientID;
    }

    public long GetStarted()
    {
        return started;
    }
}

