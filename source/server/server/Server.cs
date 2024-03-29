﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Runtime.InteropServices;
using System.Collections.Generic;

public class Server
{
    // Constants
    const int MIN_CONNECTIONS = 1;
    const int MAX_CONNECTIONS = 8;

    // Variables
    private Socket serverSocket;
    private List<TCPConnection> tcpConnections;
    private List<int> clientIDConnections;

    private int maxClientID;
    private Dictionary<int,string> clientIPs;

    private State serverState;

    // Constructor
    public Server()
    {
        /* ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ */
        /* This enclosed section is from: Stack Overflow (2011) Get Local IP Address. Available at https://stackoverflow.com/questions/6803073/get-local-ip-address (Accessed: 17 January 2023) */
        var host = Dns.GetHostEntry(Dns.GetHostName()); // CHECKME: Is System.Net.Dns too advanced?
        string internetworkIP = "not found";
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork) // DEBUG: AddressFamily.Internetwork gives 'global' address...
            {
                internetworkIP = ip.ToString();
            }
        }
        /* ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ */

        IPAddress ipAddress = IPAddress.Parse(internetworkIP);
        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 5555);

        serverSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        serverSocket.Bind(localEndPoint);
        serverSocket.Listen();

        tcpConnections = new List<TCPConnection>();
        clientIDConnections = new List<int>();
        clientIPs = new Dictionary<int, string>();
        maxClientID = -1;

        serverState = new State(0);

        // DEBUG:
        //Console.WriteLine("Server ready at " + DateTime.UtcNow.Ticks + "...");

        Console.WriteLine("Server's Internetwork IP is " + internetworkIP + "..."); // Client uses this to check... they're on the same network? // FIXME: Justify/build lobby entry using knowledge of how IP works...
        if (internetworkIP.Equals("not found"))
            Console.WriteLine("\t...Please run on a different network");// Include note in README on secret, client-side, "127.0.0.1" override...
        Console.WriteLine();
    }

    // Destructor
    ~Server()
    {
        serverSocket.Close();
    }

    // Public functions
    public void Read()
    {
        // The structure that describes the set of sockets we're interested in reading from.
        List<Socket> readable = new List<Socket>();

        // The structure that describes the set of sockets we're interested in writing to.
        List<Socket> writeable = new List<Socket>();

        readable.Add(serverSocket);

        foreach (TCPConnection tcpConnection in tcpConnections)
        {
            if (tcpConnection.IsRead())
                readable.Add(tcpConnection.GetSocket());
            if (tcpConnection.IsWrite())
                writeable.Add(tcpConnection.GetSocket());
        }

        Socket.Select(readable, writeable, null, 10000);
        //Console.WriteLine(tcpConnections.Count + " clients, " + readable.Count + " are ready to read.");

        if (readable.Contains(serverSocket))
            try
            {
                Socket clientSocket = serverSocket.Accept();

                // 'Formal' rejection introduced later! 
                tcpConnections.Add(new TCPConnection(clientSocket));
                clientIDConnections.Add(-1);

            }
            catch
            {

            }

        // Check each of the clients.
        for (int i = 0; i < tcpConnections.Count;)
        {
            bool dead = false;

            if (readable.Contains(tcpConnections[i].GetSocket()))
                dead |= tcpConnections[i].Read();

            if (dead)
            {
                Disconnect(i);
            }
            else
            {
                i++;
            }
        }
    }

    public void Update()
    {
        // STEP 1: Process all packets received
        for (int i = tcpConnections.Count-1; i >= 0; i--)
        {
            if (tcpConnections[i].disconnect)
            {
                // Only applies *AFTER* formal rejection!
                Disconnect(i);
                continue;
            }

            while (tcpConnections[i].isRecvPacket())
            {
                SendablePacket packet = tcpConnections[i].RecvPacket();
                ReceivePacket(packet, i);
            }
        }

        // STEP 2: Process changes to state (including those changed by server?)
        // ...Or will this just be handled by send/receives? Barring minimal points like time intervals, etc...
        // ...Or will this just use certain "Server 'Responses'" as appropriate?
    }

    public void Write()
    {
        // The structure that describes the set of sockets we're interested in reading from.
        List<Socket> readable = new List<Socket>();

        // The structure that describes the set of sockets we're interested in writing to.
        List<Socket> writeable = new List<Socket>();

        readable.Add(serverSocket);

        foreach (TCPConnection tcpConnection in tcpConnections)
        {
            if (tcpConnection.IsRead())
                readable.Add(tcpConnection.GetSocket());
            if (tcpConnection.IsWrite())
                writeable.Add(tcpConnection.GetSocket());
        }

        Socket.Select(readable, writeable, null, 10000);

        // Check each of the clients.
        for (int i = 0; i < tcpConnections.Count();)
        {
            bool dead = false;

            if (writeable.Contains(tcpConnections[i].GetSocket()))
            {
                dead |= tcpConnections[i].Write();
            }

            if (dead)
            {
                Disconnect(i);
            }
            else
            {
                i++;

            }
        }
    }

    private void Disconnect(int index) // FIXME: Check this works with 2+ clients...
    {
        // DEBUG:
        Console.WriteLine("Client " + clientIDConnections[index] + " is being disconnected");

        tcpConnections[index].GetSocket().Dispose(); // FIXME: Difference between close and dispose?
        tcpConnections.RemoveAt(index);

        if (clientIDConnections[index] >= 0)
        {
            for (int i = 0; i < tcpConnections.Count; i++)
            {
                HeaderPacket header = new HeaderPacket(1003);
                IDPacket id = new IDPacket(clientIDConnections[index], clientIPs[clientIDConnections[index]].ToCharArray());
                SendablePacket packet = new SendablePacket(header, Packet.Serialise<IDPacket>(id));
                tcpConnections[i].SendPacket(packet);
            }
        }

        clientIPs.Remove(clientIDConnections[index]); // DEBUG: Comment this out for more 'memory'
        clientIDConnections.RemoveAt(index); // DEBUG: Comment this out for more 'memory'

        // DEBUG:
        if (clientIDConnections.Count > 0)
        {
            Console.Write("\tWe are connected to clients [");
            for (int i = 0; i < clientIDConnections.Count - 1; i++)
                Console.Write(clientIDConnections[i] + ", ");
            Console.WriteLine(clientIDConnections[clientIDConnections.Count - 1] + "]...");
        }
        else
        {
            Console.WriteLine("\tWe are connected to no clients...");
        }
        if (clientIPs.Count > 0)
        {
            Console.Write("\tWe remember clients [");
            for (int i = 0; i < clientIPs.Count - 1; i++)
                Console.Write(clientIPs.Keys.ElementAt(i) + ", ");
            Console.WriteLine(clientIPs.Keys.ElementAt(clientIPs.Count-1)+"]...");
        }
        else
        {
            Console.WriteLine("\tWe remember no clients...");
        }
        Console.WriteLine();

        // If we are connected to no clients, we return to the lobby...
        if (tcpConnections.Count == 0)
            Receive3200();

        return;
    }

    // Client 'Calls'
    private void ReceivePacket(SendablePacket packet, int index)
    {
        // Catch-all for all requests a client could send to the server
        // All deserialisation is handled in this function
        // NB: Some header.bodyIDs aren't included here, as these will only be sent server-to-client

        // DEBUG:
        Console.WriteLine("Received packet " + packet.header.bodyID + "...");

        switch (packet.header.bodyID)
        {
            case 1000:
                SyncPacket syncPacket = Packet.Deserialise<SyncPacket>(packet.serialisedBody);
                Receive1000(packet.header.timestamp, index);
                break;
            case 1001:
                IDPacket idPacket = Packet.Deserialise<IDPacket>(packet.serialisedBody);
                Receive1001(idPacket.clientID, string.Join("", idPacket.clientIP), index);
                break;
            case 2300:
                // FIXME: How do we handle actually starting a game?
                break;
            case 2310:
                Receive2310(); // NB: Client does most of *its* initialisation on receiving this; due to TCP's A before B before C, it will have all necessary information (right?)
                break;
            case 3200:
                // FIXME: How do we handle actually starting a lobby?
                break;
            case 4101: // CHECKME: This will (evenutally) be UDP - but still a server/client connection?
                PositionPacket positionPacket = Packet.Deserialise<PositionPacket>(packet.serialisedBody);
                Receive4101(positionPacket.submarineID, positionPacket.x, positionPacket.y, positionPacket.theta, positionPacket.timestamp, index);
                break;
            case 4102:
                MorsePacket morsePacket = Packet.Deserialise<MorsePacket>(packet.serialisedBody);
                Receive4102(morsePacket.senderID, morsePacket.receiverID, morsePacket.dot, morsePacket.range, morsePacket.angle, morsePacket.interval);
                break;
            case 4190:
                AudioPacket audioPacket = Packet.Deserialise<AudioPacket>(packet.serialisedBody); // CHECKME: Do we actually need to deserialise this if we aren't modifying the sound?
                Receive4190(audioPacket.clientID, audioPacket.x, audioPacket.y, index);
                break;
        }

        // DEBUG:
        Console.WriteLine();
    }

    private void Receive1000(long syncTimestamp, int index)
    {
        // Client 'bounces packet off' server to synchronise DateTime.UtcNow.Ticks
        // Server does not need to commit this to memory
        HeaderPacket header = new HeaderPacket(1000);
        SyncPacket sync = new SyncPacket(syncTimestamp);
        SendablePacket packet = new SendablePacket(header, Packet.Serialise<SyncPacket>(sync));
        tcpConnections[index].SendPacket(packet);
    }

    private void Receive1001(int clientID, string clientIP, int index)
    {
        // Client sends the server its ID and (FIXME: currently, local) IP address, to be approved and recorded
        // Server accepts, reassigns or rejects its ID, and sends this back
        // FIXME: How will we handle case where client is trying to replace a leaver?
        bool newClient = clientID < 0;
        bool newConnection = clientIDConnections[index] == -1;

        // A robust way of rejecting clients...
        if (tcpConnections.Count() > MAX_CONNECTIONS || serverState.mode != State.Mode.lobby) // Only allow joiners in lobby!
        {
            // DEBUG
            Console.WriteLine("\tClient "+clientID+" rejected: "+((tcpConnections.Count >= MAX_CONNECTIONS) ? "Too many players!" : "Mid-game!"));

            HeaderPacket rejectionHeader = new HeaderPacket(1001);
            IDPacket rejectionID = new IDPacket(-1, clientIP.ToCharArray());
            SendablePacket rejectionPacket = new SendablePacket(rejectionHeader, Packet.Serialise<IDPacket>(rejectionID));
            tcpConnections[index].SendPacket(rejectionPacket);
            tcpConnections[index].disconnect = true; // Disconnects *after* send!

            return;
        }


        // If our client is new, assign a unique clientID
        clientIDConnections[index] = (clientID >= 0) ? clientID : ++maxClientID;
        if (!clientIPs.ContainsKey(clientIDConnections[index]))
            clientIPs.Add(clientIDConnections[index], clientIP);
        //FIXME: if condition to remove client ID/IP if over MAX_CONNECTIONS?

        // Confirm/reject client entry
        // FIXME: Currently, we are only accepting new players when in the lobby... not that sophisticated...
        //if (serverState.mode != State.Mode.lobby)
        //    clientIDConnections[index] = -1;

        //bool rejection = clientIDConnections[index] == -1;

        HeaderPacket header = new HeaderPacket(1001);
        IDPacket id = new IDPacket(clientIDConnections[index], clientIPs[clientIDConnections[index]].ToCharArray());
        SendablePacket packet = new SendablePacket(header, Packet.Serialise<IDPacket>(id));
        tcpConnections[index].SendPacket(packet);

        //if (rejection)
        //    return;

        // Send client details of all other clients, and vice versa
        // We use two different sizes of for loop to account for any 'missing' clients
        for (int i = 0; i < tcpConnections.Count; i++)
        {
            if (i == index)
                continue;

            // Sending to other clients...
            header = new HeaderPacket(1002);
            id = new IDPacket(clientIDConnections[index], clientIPs[clientIDConnections[index]].ToCharArray());
            packet = new SendablePacket(header, Packet.Serialise<IDPacket>(id));
            tcpConnections[i].SendPacket(packet);
        }

        foreach (int key in clientIPs.Keys)
        {
            if (key == clientIDConnections[index])
                continue;

            // ...And vice versa
            header = new HeaderPacket(1002);
            id = new IDPacket(key, clientIPs[key].ToCharArray());
            packet = new SendablePacket(header, Packet.Serialise<IDPacket>(id));
            tcpConnections[index].SendPacket(packet);
        }


        if (serverState.mode == State.Mode.lobby) // No one can rejoin mid-game - but their icon will be left on screen!
        {
            header = new HeaderPacket(1201);
            packet = new SendablePacket(header, Packet.Serialise<EmptyPacket>(new EmptyPacket()));
            tcpConnections[index].SendPacket(packet);
        }
    }

    private void Receive2300()
    {
        // STEP 0: Initialise match conditions
        if (tcpConnections.Count < MIN_CONNECTIONS)
            return;

        serverState.StartMatch(clientIPs.Keys.ToList(), clientIPs.Values.ToList());

        // STEP 1: Send client IP details to one another, as necessary

        // STEP 2: Send each client all initial positions

        // FIXME: Should we wait for a 'player ready!' packet (or timeout?) from each player to set global timestamp?
    }

    private void Receive2310()
    {
        // STEP 0: Initialise match conditions
        serverState.StartSandbox(clientIPs.Keys.ToList(), clientIPs.Values.ToList());
        // FIXME: Presumably - sandbox settings allow each player to choose their (preferred?) role?

        // STEP 1: Send client role, IP details to one another, as necessary
        HeaderPacket header = new HeaderPacket(2310);
        EmptyPacket empty = new EmptyPacket();
        SendablePacket packet = new SendablePacket(header, Packet.Serialise<EmptyPacket>(empty));
        for (int i = 0; i < tcpConnections.Count; i++)
            tcpConnections[i].SendPacket(packet);

        // DEBUG:
        Console.WriteLine("\tSent packet 2310 to all clients...");

        foreach (State.Superpower superpower in serverState.fleets.Keys)
        {
            int superpowerID;
            switch (superpower)
            {
                case State.Superpower.East:
                    superpowerID = 0;
                    break;
                case State.Superpower.West:
                    superpowerID = 1;
                    break;
                default:
                    superpowerID = -1;
                    break;
            }

            header = new HeaderPacket(4000);
            RolePacket role = new RolePacket(superpowerID,serverState.fleets[superpower].diplomat.clientID);
            packet = new SendablePacket(header, Packet.Serialise<RolePacket>(role));
            for (int i = 0; i < tcpConnections.Count; i++)
                tcpConnections[i].SendPacket(packet);

            foreach (int submarineID in serverState.fleets[superpower].submarines.Keys)
            {
                header = new HeaderPacket(4100); // FIXME: No accounting for crew here
                SubmarinePacket submarine = new SubmarinePacket(superpowerID, submarineID, serverState.fleets[superpower].submarines[submarineID].captain.clientID, serverState.fleets[superpower].submarines[submarineID].nuclearCapability);
                packet = new SendablePacket(header, Packet.Serialise<SubmarinePacket>(submarine));
                for (int i = 0; i < tcpConnections.Count; i++)
                    tcpConnections[i].SendPacket(packet);

                header = new HeaderPacket(4101); // FIXME: No accounting for crew here
                PositionPacket position = new PositionPacket(submarineID, serverState.fleets[superpower].submarines[submarineID].x[2], serverState.fleets[superpower].submarines[submarineID].y[2], serverState.fleets[superpower].submarines[submarineID].theta[2], serverState.fleets[superpower].submarines[submarineID].timestamp[2]);
                packet = new SendablePacket(header, Packet.Serialise<PositionPacket>(position));
                for (int i = 0; i < tcpConnections.Count; i++)
                    if (true) // FIXME: Add extra, proximity condition!
                        tcpConnections[i].SendPacket(packet); // Note: Submarine receives its own position here, as it needs initialised!
            }
        }

        header = new HeaderPacket(2311);
        empty = new EmptyPacket();
        packet = new SendablePacket(header, Packet.Serialise<EmptyPacket>(empty));
        for (int i = 0; i < tcpConnections.Count; i++)
            tcpConnections[i].SendPacket(packet);

        // DEBUG:
        Console.WriteLine("\tSent packet 2311 to all clients...");
    }

    private void Receive3200()
    {
        // Need to 'wipe' remaining clients of their knowledge, connections, etc...
        tcpConnections = new List<TCPConnection>();
        clientIDConnections = new List<int>();
        clientIPs = new Dictionary<int, string>();

        serverState.StartLobby();
    }

    private void Receive4101(int submarineID, float x, float y, float theta, long timestamp, int index)
    {
        // Captain sends the server their new position
        // Server updates its current state, then forwards on to all other clients
        serverState.UpdateSubmarine(submarineID, x, y, theta, timestamp);

        HeaderPacket header = new HeaderPacket(4101);
        PositionPacket submarine = new PositionPacket(submarineID, x, y, theta, timestamp);
        SendablePacket packet = new SendablePacket(header, Packet.Serialise<PositionPacket>(submarine));
        for (int i = 0; i < tcpConnections.Count; i++)
            if (i != index) // FIXME: No discretion about who we send to could mean spam?
                tcpConnections[i].SendPacket(packet);
    }

    private void Receive4102(int senderID, int receiverID, bool dot, float range, float angle, long interval)
    {
        HeaderPacket header = new HeaderPacket(4102);
        MorsePacket morse = new MorsePacket(senderID, receiverID, dot, range, angle, interval);
        SendablePacket packet = new SendablePacket(header, Packet.Serialise<MorsePacket>(morse));

        Dictionary<int,Submarine> submarines = serverState.GetSubmarines();
        if (submarines.ContainsKey(receiverID))
        {
            int index = clientIDConnections.FindIndex(x => x == submarines[receiverID].captain.clientID);
            if (index >= 0) // Ignore if the client is not currently connected!
                tcpConnections[index].SendPacket(packet);
        }
    }

    private void Receive4190(int clientID, float x, float y, int index)
    {
        // MVP IMPLEMENTATION: Can we send audio to and from the same client?
        HeaderPacket header = new HeaderPacket(4190);
        AudioPacket audio = new AudioPacket(clientID, x, y);
        SendablePacket packet = new SendablePacket(header, Packet.Serialise<AudioPacket>(audio));
        tcpConnections[index].SendPacket(packet);
    }
}
