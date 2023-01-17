using Godot;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Runtime.InteropServices;
using System.Collections.Generic;

public class Client : Node
{
    [Signal] delegate void ReceivedPacket(int packetID);
    [Signal] delegate void ReceivedKick();
    [Signal] delegate void ReceivedSoundwaveCollision(int senderID, bool collisionDot, float collisionRange, float collisionAngle, long collisionTicks);

    [Signal] delegate void ReceivedFrame(Vector2 frame);

    private const string CLIENTIP = "127.0.0.1";// FIXME: How to get own IP?
    private const string SERVERIP = "192.168.1.200";//"80.44.238.161";
    private const int SERVERPORT = 5555;
    private const int DELAY_SAMPLE_SIZE = 5;

    // Variables
    private Socket clientSocket;
    private TCPConnection serverConnection;
    private bool disconnected;

    private string clientIP;
    private int clientID;
    private List<int> clientIDs;
    private Dictionary<int,string> clientIPs;
    public State state;

    private long started;
    public List<long> delaySamples;
    public long delay;

    public bool sandboxBlocking;

    public int submarineID;

    // DEBUG
    //private int position_counter;

    // Constructor
    public Client()
    {
        clientSocket = new Socket(IPAddress.Parse(CLIENTIP).AddressFamily,SocketType.Stream,ProtocolType.Tcp); 

        serverConnection = new TCPConnection(clientSocket);
        disconnected = true;

        /* ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ */
        /* This enclosed section is from: Stack Overflow (2011) Get Local IP Address. Available at https://stackoverflow.com/questions/6803073/get-local-ip-address (Accessed: 17 January 2023) */
        clientIP = "";
        var host = Dns.GetHostEntry(Dns.GetHostName()); // CHECKME: Is System.Net.Dns too advanced?
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork) // DEBUG: AddressFamily.Internetwork gives 'global' address...
            {
                clientIP = ip.ToString();
            }
        }
        /* ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ */

        // Initialise for New Game 
        clientID = -1;
        clientIDs = new List<int>();
        clientIPs = new Dictionary<int, string>();

        state = new State(State.Mode.lobby, 0); // Seed doesn't matter on client side (?)

        started = 0;

        delaySamples = new List<long>();
        delay = 0;

        sandboxBlocking = false;

        submarineID = -1;

    }

    // Destructor
    ~Client()
    {
        serverConnection.GetSocket().Dispose();
        clientSocket.Dispose();
    }

    // Accessors
    public bool IsConnected()
    {
        return !disconnected;
    }

    // Public functions
    public bool Connect(string serverIP)
    {
        try
        {
            clientSocket.ConnectAsync(new IPEndPoint(IPAddress.Parse(serverIP),SERVERPORT));
            disconnected = !clientSocket.Poll(2000000, SelectMode.SelectWrite); // Happy to disallow latencies higher than 2s!

            if (!disconnected) // Immediately sync on connection...
            {
                HeaderPacket header = new HeaderPacket(1000);
                SyncPacket sync = new SyncPacket(0);
                SendablePacket packet = new SendablePacket(header,Packet.Serialise<SyncPacket>(sync));
                serverConnection.SendPacket(packet);
                return true;
            }
            else
            {
                GD.Print("Failed to connect to server...");
                return false;
            }
        }
        catch
        {
            GD.Print("Error in connecting to server...");
            return false;
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

        Socket.Select(readable, writeable, null, 10000);

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

        Socket.Select(readable, writeable, null, 10000);

        if (writeable.Contains(serverConnection.GetSocket()))
            disconnected |= serverConnection.Write();
    }

    public void Reset()
    {
        disconnected = true;

        clientSocket.Dispose();
        serverConnection.GetSocket().Dispose();

        clientSocket = new Socket(IPAddress.Parse(CLIENTIP).AddressFamily,SocketType.Stream,ProtocolType.Tcp); 
        serverConnection = new TCPConnection(clientSocket);

        clientIDs = new List<int>();
        clientIPs = new Dictionary<int, string>();

        state = new State(State.Mode.lobby, 0);

        delaySamples = new List<long>();
        delay = 0;
        
        sandboxBlocking = false;

        submarineID = -1;
    }

    // Server 'Calls'
    private void ReceivePacket(SendablePacket packet)
    {
        // Catch-all for all requests a client could send to the server
        // All deserialisation is handled in this function
        // NB: Some header.bodyIDs aren't included here, as these will only be sent server-to-client

        // DEBUG:
        GD.Print("Received packet " + packet.header.bodyID + "...");

        SyncPacket syncPacket;
        IDPacket idPacket;
        RolePacket rolePacket;
        SubmarinePacket submarinePacket;
        PositionPacket positionPacket;
        AudioPacket audioPacket;
        MorsePacket morsePacket;
        switch (packet.header.bodyID)
        {
            case 1000:
                syncPacket = Packet.Deserialise<SyncPacket>(packet.serialisedBody);
                Receive1000(packet.header.timestamp, syncPacket.syncTimestamp);
                break;
            case 1001:
                idPacket = Packet.Deserialise<IDPacket>(packet.serialisedBody);
                Receive1001(idPacket.clientID, string.Join("",idPacket.clientIP));
                break;
            case 1002:
                idPacket = Packet.Deserialise<IDPacket>(packet.serialisedBody);
                Receive1002(idPacket.clientID, string.Join("",idPacket.clientIP));
                break;
            case 1003:
                idPacket = Packet.Deserialise<IDPacket>(packet.serialisedBody);
                Receive1003(idPacket.clientID);
                break;
            case 1201:
                Receive1201();
                break;
            case 2300:
                // FIXME: Cues initialisation...
                break;
            case 2301:
                // FIXME: How do we handle actually starting a game?
                break;
            case 2310:
                // FIXME: Cues initialisation...
                Receive2310();
                break;
            case 2311:
                Receive2311();
                break;
            case 3200:
                // FIXME: Cues initialisation...
                break;
            case 3201:
                // FIXME: How do we handle actually starting a lobby?
                break;
            case 4000:
                rolePacket = Packet.Deserialise<RolePacket>(packet.serialisedBody);
                Receive4000(rolePacket.superpowerID,rolePacket.clientID);
                break;
            case 4100:
                submarinePacket = Packet.Deserialise<SubmarinePacket>(packet.serialisedBody);
                Receive4100(submarinePacket.superpowerID,submarinePacket.submarineID,submarinePacket.clientID,submarinePacket.nuclearCapability);
                break;
            case 4101: // CHECKME: This will (evenutally) be UDP - but still a server/client connection? // Or - using 'forward-only' prediction, hence no UDP!
                positionPacket = Packet.Deserialise<PositionPacket>(packet.serialisedBody);
                Receive4101(positionPacket.submarineID, positionPacket.x, positionPacket.y, positionPacket.theta, positionPacket.timestamp);
                break;
            case 4102:
                morsePacket = Packet.Deserialise<MorsePacket>(packet.serialisedBody);
                Receive4102(morsePacket.senderID,morsePacket.dot,morsePacket.range,morsePacket.angle,morsePacket.interval);
                break;
            case 4190:
                audioPacket = Packet.Deserialise<AudioPacket>(packet.serialisedBody);
                Receive4190(audioPacket.clientID, audioPacket.x, audioPacket.y);
                break; 
        }
        EmitSignal("ReceivedPacket",packet.header.bodyID);

        // DEBUG:
        GD.Print();
    }

    private void Receive1000(long serverTimestamp, long syncTimestamp)
    {
        // Estimating the time to get from client to server, or vice versa...
        delaySamples.Add((DateTime.UtcNow.Ticks-syncTimestamp)/2); // Round trip time...
        if (delaySamples.Count < DELAY_SAMPLE_SIZE) // Sample delay multiple times, for more reliable estimate...
        {
            HeaderPacket syncHeader = new HeaderPacket(1000);
            SyncPacket sync = new SyncPacket(0);
            SendablePacket syncPacket = new SendablePacket(syncHeader,Packet.Serialise<SyncPacket>(sync));
            serverConnection.SendPacket(syncPacket);
            return;
        }

        delay = 0;
        foreach (int delaySample in delaySamples)
            delay += delaySample;
        delay /= DELAY_SAMPLE_SIZE;

        // DEBUG:
        GD.Print("We are a delay of "+delay+" behind the server...");

        // Now that we've (re-)synced, we send our initial ID Packet...
        HeaderPacket header = new HeaderPacket(1001);
        IDPacket id = new IDPacket(clientID,clientIP.ToCharArray());
        SendablePacket packet = new SendablePacket(header,Packet.Serialise<IDPacket>(id));
        serverConnection.SendPacket(packet);
    }

    private void Receive1001(int init_clientID, string init_clientIP)
    {
        GD.Print(init_clientID);

        // Client receives ID confirmation/rejection
        if (init_clientID < 0)
        {
            GD.Print(init_clientID);

            EmitSignal("ReceivedKick");
            Reset();
            return;
        }

        clientID = init_clientID;

        if (clientIPs.ContainsKey(clientID)) // Client has been 'formally' rejected...
              return;

        clientIPs.Add(init_clientID,init_clientIP);

        // FIXME: Ask for server details? - No, just wait for server to say we're... initialising whichever mode!
    }

    private void Receive1002(int init_clientID, string init_clientIP)
    {
        if (init_clientID < 0 || clientIPs.ContainsKey(init_clientID))
            return;

        clientIDs.Add(init_clientID);
        clientIPs.Add(init_clientID,init_clientIP);

        Console.WriteLine(clientIPs+" "+clientIPs[init_clientID]);
    }

    private void Receive1003(int init_clientID)
    {
        if (clientIDs.Contains(init_clientID))
            clientIDs.Remove(init_clientID);

        if (clientIPs.ContainsKey(init_clientID))
            clientIPs.Remove(init_clientID);

        // If the client controls a submarine, stop predicting any movement...
        Dictionary<int, Submarine> submarines = state.GetSubmarines();
        long interpolationTimestamp = (submarineID >= 0) ? state.GetSubmarines()[submarineID].timestamp[2] : DateTime.UtcNow.Ticks; 
        foreach (int submarineID in submarines.Keys)
            if (submarines[submarineID].captain.clientID == init_clientID)
                for (int i = 0; i < 3; i++) // Brings abandoned submarine fully to rest // CHECKME: Erroneous use of interpolationTimestamp?
                    state.UpdateSubmarine(submarineID,submarines[submarineID].x[2],submarines[submarineID].y[2],submarines[submarineID].theta[2],submarines[submarineID].timestamp[2]+1,interpolationTimestamp);
        
        // No need to handle rejoining submarines...
    }

    private void Receive1200()
    {
        state = new State(State.Mode.lobby, 0); // NB: This should be fine, as client does not use RNG...
    }

    private void Receive1201()
    {
        // FIXME: Initialise state/lobby settings!
    }

    private void Receive2310()
    {
        // Client receives cues that they're entering sandbox mode
        state.mode = State.Mode.sandbox;

    }

    private void Receive2311()
    {
        // START! - With UDP, this involves establishing connections...
        sandboxBlocking = false;

        // DEBUG:
        GD.Print("\tClient "+clientID+" has started in the following state:");
        foreach (State.Superpower superpower in state.fleets.Keys)
        {
            string superpowerText = "";
            switch (superpower)
            {
                case State.Superpower.East:
                    superpowerText = "The Eastern Bloc";
                    break;
                case State.Superpower.West:
                    superpowerText = "The Western Bloc";
                    break;
                case State.Superpower.Null:
                    superpowerText = "The Null Bloc";
                    break;
            }

            string diplomatAddress = "";
            if (state.fleets[superpower].diplomat.clientIP.Length > 0)
                diplomatAddress = "with IP Address "+state.fleets[superpower].diplomat.clientIP;
            else
                diplomatAddress = "with no known IP Address";   

            GD.Print("\t\t"+superpowerText+" has Client "+state.fleets[superpower].diplomat.clientID+" as its diplomat, "+diplomatAddress+"...");

            foreach (int submarineID in state.fleets[superpower].submarines.Keys)
            {
                GD.Print("\t\tSubmarine "+submarineID+" starts at ("+state.fleets[superpower].submarines[submarineID].x[2]+", "+state.fleets[superpower].submarines[submarineID].y[2]+"), angle "+state.fleets[superpower].submarines[submarineID].theta[2]);

                string captainAddress = "";
                if (state.fleets[superpower].submarines[submarineID].captain.clientIP.Length > 0)
                    captainAddress = ""+state.fleets[superpower].submarines[submarineID].captain.clientIP;
                else
                    captainAddress = "unknown"; 

                GD.Print("\t\t\tCaptain "+state.fleets[superpower].submarines[submarineID].captain.clientID+", IP Address "+captainAddress+"...");
            }
        }

        started = state.GetSubmarines()[submarineID].timestamp[2];

        // DEBUG:
        //position_counter = 0;
    }

    private void Receive4000(int superpowerID, int diplomatID)
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

        try
        {
            state.AddFleet(superpower,diplomatID,clientIPs[diplomatID]);
        }
        catch
        {
            state.AddFleet(superpower,diplomatID,""); // FIXME: Add 'query' call to get another player's ID?
        }
    }

    private void Receive4100(int init_superpowerID, int init_submarineID, int init_captainID, bool init_nuclearCapability)
    {
        State.Superpower superpower;
        switch (init_superpowerID)
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

        state.AddSubmarine(superpower,init_submarineID,init_captainID,clientIPs[init_captainID],init_nuclearCapability);
        if (init_captainID == clientID)
            submarineID = init_submarineID;

    }

    private void Receive4101(int init_submarineID, float init_x, float init_y, float init_theta, long init_timestamp)
    {
        // FIXME: Add range checks on server and client sides...
        // FIXME: Who should get priority if client and server update the submarine *at the same time*? This is built in to Submarine.cs, to some extent...
        long interpolationTimestamp = (submarineID >= 0) ? state.GetSubmarines()[submarineID].timestamp[2] : DateTime.UtcNow.Ticks; // This timestamp keeps up with the current frame
        state.UpdateSubmarine(init_submarineID,init_x,init_y,init_theta,init_timestamp,interpolationTimestamp,delay);

        // DEBUG:
        //if (init_game > 0 && (position_counter++)%10 == 0)
        //    GD.Print(init_timestamp-started);

    }

    private void Receive4102(int senderID, bool dot, float range, float angle, long interval)
    {
        EmitSignal("ReceivedSoundwaveCollision",senderID,dot,range,angle,interval);
    }

    private void Receive4190(int init_clientID, float init_x, float init_y)
    {
        //GD.Print("Audio received!");
    }

    // Client 'Calls'
    public void Send1003()
    {
        // Client leaves a server manually
        HeaderPacket header = new HeaderPacket(1003);
        EmptyPacket empty = new EmptyPacket();
        SendablePacket packet = new SendablePacket(header,Packet.Serialise<EmptyPacket>(empty));
        serverConnection.SendPacket(packet);
    }

    public void Send2310()
    {
        // Client sends message to server to start sandbox mode
        sandboxBlocking = true;

        HeaderPacket header = new HeaderPacket(2310);
        EmptyPacket empty = new EmptyPacket();
        SendablePacket packet = new SendablePacket(header,Packet.Serialise<EmptyPacket>(empty));
        serverConnection.SendPacket(packet);
    }

    public void Send4101(float x, float y, float theta, long timestamp)
    {
        // Client sends details of their own submarine to server
        // FIXME: Since this will never be sent erroneously, can't we remove all arguments?
        HeaderPacket header = new HeaderPacket(4101);
        PositionPacket submarine = new PositionPacket(submarineID,x,y,theta,timestamp);
        SendablePacket packet = new SendablePacket(header,Packet.Serialise<PositionPacket>(submarine));
        serverConnection.SendPacket(packet);
    }

    public void Send4102(int senderID, int receiverID, bool dot, float range, float angle, long interval)
    {
        // Client sends details of a soundwave collision to server
        HeaderPacket header = new HeaderPacket(4102);
        MorsePacket morse = new MorsePacket(senderID,receiverID,dot,range,angle,interval);
        SendablePacket packet = new SendablePacket(header,Packet.Serialise<MorsePacket>(morse));
        serverConnection.SendPacket(packet);
    }

    public void Send4190(float x, float y)
    {
        // Client sends details of a single audio frame to the server
        HeaderPacket header = new HeaderPacket(4190);
        AudioPacket audio = new AudioPacket(clientID,x,y);
        SendablePacket packet = new SendablePacket(header,Packet.Serialise<AudioPacket>(audio));
        serverConnection.SendPacket(packet);
    }

    // Accessors
    public int GetClientID()
    {
        return clientID;
    }

    public void SetClientID(int init_clientID)
    {
        clientID = init_clientID;
    }

    public List<int> GetClientIDs()
    {
        return new List<int>(clientIDs);
    }

    public string GetClientIP(int init_clientID)
    {
        return clientIPs[init_clientID];
    }

    public long GetStarted()
    {
        return started;
    }
}

