using Godot;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Runtime.InteropServices;
using System.Collections.Generic;

public class Client
{
    private const string CLIENTIP = "127.0.0.1";// FIXME: How to get own IP?
    private const string SERVERIP = "127.0.0.1";//"192.168.1.200";//"80.44.238.161";
    private const int SERVERPORT = 5555;

    // Variables
    private Socket clientSocket;
    private TCPConnection serverConnection;
    private bool disconnected;

    private int clientID;
    private Dictionary<int,string> clientIPs;
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
        state = new State(0); // Seed doesn't matter on client side (?)

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
            clientSocket.ConnectAsync(new IPEndPoint(IPAddress.Parse(SERVERIP),SERVERPORT));
            disconnected = !clientSocket.Poll(100000, SelectMode.SelectWrite); // CHECKME: Async - separate thread/simultaneously?

            if (!disconnected) // Immediately sync on connection...
            {
                HeaderPacket header = new HeaderPacket(1000);
                SyncPacket sync = new SyncPacket(0);
                SendablePacket packet = new SendablePacket(header,Packet.Serialise<SyncPacket>(sync));
                serverConnection.SendPacket(packet);
            }
        }
        catch
        {
            GD.Print("Failure!");
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

    // Server 'Calls'
    private void ReceivePacket(SendablePacket packet)
    {
        // Catch-all for all requests a client could send to the server
        // All deserialisation is handled in this function
        // NB: Some header.bodyIDs aren't included here, as these will only be sent server-to-client
        switch (packet.header.bodyID)
        {
            case 1000:
                SyncPacket syncPacket = Packet.Deserialise<SyncPacket>(packet.serialisedBody);
                Receive1000(packet.header.timestamp, syncPacket.syncTimestamp);
                break;
            case 1001:
                IDPacket idPacket = Packet.Deserialise<IDPacket>(packet.serialisedBody);
                Receive1001(idPacket.clientID);
                break;
            case 1002:
                break;
            case 2300:
                // FIXME: Cues initialisation...
                break;
            case 2301:
                // FIXME: How do we handle actually starting a game?
                break;
            case 2310:
                // FIXME: Cues initialisation...
                break;
            case 2311:
                //Receive2311();
                // FIXME: This is where we incorporate everything from the lobby!
                break;
            case 3200:
                // FIXME: Cues initialisation...
                break;
            case 3201:
                // FIXME: How do we handle actually starting a lobby?
                break;
            case 4000:
                RolePacket rolePacket = Packet.Deserialise<RolePacket>(packet.serialisedBody);
                Receive4000(rolePacket.clientID,rolePacket.superpowerID);
                break;
            case 4100:
                rolePacket = Packet.Deserialise<RolePacket>(packet.serialisedBody);
                Receive4100(rolePacket.clientID,rolePacket.superpowerID);
                break;
            case 4101: // CHECKME: This will (evenutally) be UDP - but still a server/client connection? // Or - using 'forward-only' prediction, hence no UDP!
                PositionPacket positionPacket = Packet.Deserialise<PositionPacket>(packet.serialisedBody);
                Receive4101(positionPacket.clientID, positionPacket.x, positionPacket.y, positionPacket.theta, positionPacket.timestamp);
                break;
        }
    }

    private void Receive1000(long serverTimestamp, long syncTimestamp)
    {
        // Client receives a packet 'bounced off' the server
        // Client uses syncTimestamp to estimate when the bounce occurred, then compares to serverTimestamp to estimate the delay between device clocks

        // clientMoment and serverMoment should occur at (roughly) the same time
        long clientTimestamp = (syncTimestamp+DateTime.UtcNow.Ticks)/2;

        // If clientTimestamp < serverTimestamp, the client is delayed behind the server
        // We add this delay to future client calculations, to 'catch them up' with the server
        delay = serverTimestamp-clientTimestamp;

        // DEBUG:
        Console.WriteLine("We are a delay of "+delay+" behind the server...");

        // Now that we've (re-)synced, we send our initial ID Packet...
        HeaderPacket header = new HeaderPacket(1001);
        IDPacket id = new IDPacket(clientID,CLIENTIP.ToCharArray());
        SendablePacket packet = new SendablePacket(header,Packet.Serialise<IDPacket>(id));
        serverConnection.SendPacket(packet);
    }

    private void Receive1001(int init_clientID)
    {
        // Client receives ID confirmation/rejection
        clientID = init_clientID;

        // FIXME: Ask for server details? - No, just wait for server to say we're... initialising whichever mode!
    }

    private void Receive1002(/* FIXME: ??? */)
    {

    }

    private void Receive2310()
    {
        state = new State((int)(DateTime.UtcNow.Ticks+delay));
    }

    private void Receive2311()
    {
        // START! - With UDP, this involves establishing connections...
    }

    private void Receive4000(int diplomatID, int superpowerID)
    {
        State.Superpower superpower;
        switch (superpowerID)
        {
            case 0:
                superpower = State.Superpower.East;
                break;
            case 1:
                superpower = State.Superpower.West;
                break;
            default:
                superpower = State.Superpower.Null;
                break;
        }

        state.AddDiplomat(superpower,diplomatID,clientIPs[diplomatID]);
    }

    private void Receive4100(int captainID, int superpowerID)
    {
        State.Superpower superpower;
        switch (superpowerID)
        {
            case 0:
                superpower = State.Superpower.East;
                break;
            case 1:
                superpower = State.Superpower.West;
                break;
            default:
                superpower = State.Superpower.Null;
                break;
        }

        state.AddSubmarine(superpower,captainID,clientIPs[captainID]);
    }

    private void Receive4101(int init_clientID, float init_x, float init_y, float init_theta, long init_timestamp)
    {
        // FIXME: Fill in the blank!
    }

    // Client 'Calls'

// FIXME: DEPRECATED
    // Send functions


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
    /*private void ReceivePacket(SendablePacket packet) // Redirects each type of packet
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
    }*/

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

