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
    private List<int> clientIDConnections;

    private int maxClientID;
    private Dictionary<int,string> clientIPs;

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

        /* -------------------------------------------------------------------- */
        /* CC: https://stackoverflow.com/questions/6803073/get-local-ip-address */
        var host = Dns.GetHostEntry(Dns.GetHostName()); // CHECME: Is System.
        string localIP = "undefined";
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.Unix) // FIXME: So, could we use *another* address family? // FIXME: Use local network; absolutely what is expected for purposes of this coursework...
            {
                localIP = ip.ToString();
            }
        }
        /* -------------------------------------------------------------------- */

        IPAddress ipAddress = IPAddress.Parse("127.0.0.1");// IPAddress.Parse("192.168.1.200");
        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 5555);

        serverSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        serverSocket.Bind(localEndPoint);
        serverSocket.Listen();

        tcpConnections = new List<TCPConnection>(MAX_PENDING_CONNECTIONS);
        clientIDConnections = new List<int>();
        clientIPs = new Dictionary<int, string>();
        maxClientID = -1;



        serverState = new State(0);

        // DEBUG:
        Console.WriteLine("Server ready at " + DateTime.UtcNow.Ticks + "...");
        Console.WriteLine("Server's Unix IP is " + localIP + "...");
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
                    clientIDConnections.Add(-1);
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

        for (int i = 0; i < tcpConnections.Count; i++)
        {
            HeaderPacket header = new HeaderPacket(1003);
            IDPacket id = new IDPacket(clientIDConnections[index], clientIPs[clientIDConnections[index]].ToCharArray());
            SendablePacket packet = new SendablePacket(header, Packet.Serialise<IDPacket>(id));
            tcpConnections[i].SendPacket(packet);
        }

        // DEBUG: Comment this out for more 'memory'
        //clientIPs.Remove(clientIDConnections[index]);

        clientIDConnections.RemoveAt(index);



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
        Console.Write("\tWe remember clients [");
        for (int i = 0; i < clientIPs.Count - 1; i++)
            Console.Write(clientIPs.Keys.ElementAt(i) + ", ");
        Console.WriteLine(clientIPs.Keys.ElementAt(clientIPs.Count-1)+"]...");
        Console.WriteLine();

        // If we are connected to no clients, we return to the lobby...
        if (clientIDConnections.Count == 0)
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

        // If our client is new, assign a unique clientID
        clientIDConnections[index] = (clientID >= 0) ? clientID : ++maxClientID;
        if (!clientIPs.ContainsKey(clientIDConnections[index]))
            clientIPs.Add(clientIDConnections[index], clientIP);
        //FIXME: if condition to remove client ID/IP if over MAX_CONNECTIONS?

        // Confirm/reject client entry
        // FIXME: Rejection - should this just be handled by breaking connection?
        HeaderPacket header = new HeaderPacket(1001);
        IDPacket id = new IDPacket(clientIDConnections[index], clientIPs[index].ToCharArray());
        SendablePacket packet = new SendablePacket(header, Packet.Serialise<IDPacket>(id));
        tcpConnections[index].SendPacket(packet);

        // Send client details of all other clients, and vice versa
        // We use two different sizes of for loop to account for any 'missing' clients
        for (int i = 0; i < tcpConnections.Count; i++)
        {
            if (i == index)
                continue;

            // Sending to other clients...
            header = new HeaderPacket(1002);
            id = new IDPacket(clientIDConnections[index], clientIPs[index].ToCharArray());
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
        // FIXME: Need to 'wipe' remaining clients of their knowledge, connections, etc...
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
}
