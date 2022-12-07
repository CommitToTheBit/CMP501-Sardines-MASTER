using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Runtime.InteropServices;
using System.Collections.Generic;

public class Server
{
    // Constants
    const int MIN_CONNECTIONS = 5;
    const int MAX_CONNECTIONS = 8;
    const int MAX_PENDING_CONNECTIONS = 1;

    // Variables
    private Socket serverSocket;
    private List<TCPConnection> tcpConnections;

    private int maxClientID;
    private List<int> clientIDs;
    private List<string> clientIPs;

    private State serverState;

    // Constructor
    public Server()
    {
        /*
        *   CC: https://www.c-sharpcorner.com/blogs/how-to-get-public-ip-address-using-c-sharp1, 12.42pm, 27.11.22 
        */
        /*String address = "";
        WebRequest request = WebRequest.Create("http://checkip.dyndns.org/");
        using (WebResponse response = request.GetResponse())
        using (StreamReader stream = new StreamReader(response.GetResponseStream()))
        {
            address = stream.ReadToEnd();
        }

        int first = address.IndexOf("Address: ") + 9;
        int last = address.LastIndexOf("</body>");
        address = address.Substring(first, last - first);*/

        IPAddress ipAddress = IPAddress.Parse("127.0.0.1");// IPAddress.Parse("192.168.1.200");
        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 5555);

        serverSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        serverSocket.Bind(localEndPoint);
        serverSocket.Listen();

        tcpConnections = new List<TCPConnection>(MAX_PENDING_CONNECTIONS);
        clientIDs = new List<int>();
        clientIPs = new List<string>();
        maxClientID = -1;

        serverState = new State(0);

        // DEBUG:
        Console.WriteLine("Server ready at " + DateTime.UtcNow.Ticks + "...");
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

        Socket.Select(readable, writeable, null, 1000000);
        //Console.WriteLine(tcpConnections.Count + " clients, " + readable.Count + " are ready to read.");

        if (readable.Contains(serverSocket))
            try
            {
                Socket clientSocket = serverSocket.Accept();


                if (tcpConnections.Count() <= MAX_CONNECTIONS) // FIXME: Need stricter condition to stop new joiners mid-game (i.e. clientIDs.Count() <= MAX_CONNECTIONS) // FIXME: Close socket before message sends!
                {
                    tcpConnections.Add(new TCPConnection(clientSocket));
                    clientIDs.Add(-1);
                    clientIPs.Add("");
                }
                else
                {
                    TCPConnection tcpConnection = new TCPConnection(clientSocket);
                    tcpConnection.GetSocket().Dispose();
                    return; // FIXME: Is there any need to return here?
                }

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
                tcpConnections[i].GetSocket().Dispose(); // FIXME: Difference between close and dispose?
                tcpConnections.RemoveAt(i);
                clientIDs.RemoveAt(i);
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
        for (int i = 0; i < tcpConnections.Count; i++)
            while (tcpConnections[i].isRecvPacket())
            {
                SendablePacket packet = tcpConnections[i].RecvPacket();
                ReceivePacket(packet,i);
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

        Socket.Select(readable, writeable, null, 1000000);

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
                tcpConnections[i].GetSocket().Dispose();
                tcpConnections.RemoveAt(i);
                clientIDs.RemoveAt(i);
            }
            else
            {
                i++;

            }
        }
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
                Receive4101(positionPacket.clientID, positionPacket.x, positionPacket.y, positionPacket.theta, positionPacket.timestamp, index);
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
        bool newConnection = clientIDs[index] == -1;

        // If our client is new, assign a unique clientID
        clientIDs[index] = (clientID >= 0) ? clientID : ++maxClientID;
        clientIPs[index] = clientIP;
        //FIXME: if condition to remove client ID/IP if over MAX_CONNECTIONS?

        // Confirm/reject client entry
        // FIXME: Rejection - should this just be handled by breaking connection?
        HeaderPacket header = new HeaderPacket(1001);
        IDPacket id = new IDPacket(clientIDs[index], clientIPs[index].ToCharArray());
        SendablePacket packet = new SendablePacket(header, Packet.Serialise<IDPacket>(id));
        tcpConnections[index].SendPacket(packet);

        // Send client details of all other clients, and vice versa
        for (int i = 0; i < tcpConnections.Count; i++)
        {
            if (i == index)
                continue;

            // Sending to other clients...
            header = new HeaderPacket(1002);
            id = new IDPacket(clientIDs[index], clientIPs[index].ToCharArray());
            packet = new SendablePacket(header, Packet.Serialise<IDPacket>(id));
            tcpConnections[i].SendPacket(packet);

            // ...And vice versa
            header = new HeaderPacket(1002);
            id = new IDPacket(clientIDs[i], clientIPs[i].ToCharArray());
            packet = new SendablePacket(header, Packet.Serialise<IDPacket>(id));
            tcpConnections[index].SendPacket(packet);
        }
    }

    private void Receive2300()
    {
        // STEP 0: Initialise match conditions
        if (tcpConnections.Count < MIN_CONNECTIONS)
            return;

        serverState.StartMatch(clientIDs, clientIPs);

        // STEP 1: Send client IP details to one another, as necessary

        // STEP 2: Send each client all initial positions

        // FIXME: Should we wait for a 'player ready!' packet (or timeout?) from each player to set global timestamp?
    }

    private void Receive2310()
    {
        // STEP 0: Initialise match conditions
        serverState.StartSandbox(clientIDs, clientIPs);
        // FIXME: Presumably - sandbox settings allow each player to choose their (preferred?) role?

        // STEP 1: Send client role, IP details to one another, as necessary
        HeaderPacket header = new HeaderPacket(2310);
        EmptyPacket empty = new EmptyPacket();
        SendablePacket packet = new SendablePacket(header, Packet.Serialise<EmptyPacket>(empty));
        for (int i = 0; i < tcpConnections.Count; i++)
            tcpConnections[i].SendPacket(packet);

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
            RolePacket role = new RolePacket(serverState.fleets[superpower].diplomat.clientID,superpowerID);
            packet = new SendablePacket(header, Packet.Serialise<RolePacket>(role));
            for (int i = 0; i < tcpConnections.Count; i++)
                tcpConnections[i].SendPacket(packet);

            foreach (int clientID in serverState.fleets[superpower].submarines.Keys)
            {
                header = new HeaderPacket(4100); // FIXME: No accounting for crew here
                role = new RolePacket(clientID, superpowerID);
                packet = new SendablePacket(header, Packet.Serialise<RolePacket>(role));
                for (int i = 0; i < tcpConnections.Count; i++)
                    tcpConnections[i].SendPacket(packet);

                header = new HeaderPacket(4101); // FIXME: No accounting for crew here
                PositionPacket positionPacket = new PositionPacket(clientID, serverState.fleets[superpower].submarines[clientID].x[2], serverState.fleets[superpower].submarines[clientID].y[2], serverState.fleets[superpower].submarines[clientID].theta[2], serverState.fleets[superpower].submarines[clientID].timestamp[2]);
                packet = new SendablePacket(header, Packet.Serialise<RolePacket>(role));
                for (int i = 0; i < tcpConnections.Count; i++)
                    if (i != clientID) // FIXME: Add extra, proximity condition!
                        tcpConnections[i].SendPacket(packet);
            }
        }

        header = new HeaderPacket(2311);
        empty = new EmptyPacket();
        packet = new SendablePacket(header, Packet.Serialise<EmptyPacket>(empty));
        for (int i = 0; i < tcpConnections.Count; i++)
            tcpConnections[i].SendPacket(packet);
    }

    private void Receive3200()
    {
        // FIXME: Need to 'wipe' remaining clients of their knowledge, connections, etc...
    }

    private void Receive4101(int clientID, float x, float y, float theta, long timestamp, int index)
    {
        // Captain sends the server their new position
        // Server updates its current state, then forwards on to all other clients
        serverState.UpdateSubmarine(clientID, x, y, theta, timestamp);

        HeaderPacket header = new HeaderPacket(4101);
        PositionPacket submarine = new PositionPacket(clientID, x, y, theta, timestamp);
        SendablePacket packet = new SendablePacket(header, Packet.Serialise<PositionPacket>(submarine));
        for (int i = 0; i < tcpConnections.Count; i++)
            if (i != index) // FIXME: No discretion about who we send to could mean spam?
                tcpConnections[i].SendPacket(packet);
    }
}
