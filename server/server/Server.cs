using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace server
{
    public class Server
    {
        // Variables
        private Socket serverSocket;

        private List<TCPConnection> tcpConnections;
        private List<int> clientIDs;
        private int maxClientID;

        private State serverState;

        private long started;


        // Constructor
        public Server()
        {
            IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 5555);

            serverSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(localEndPoint);
            serverSocket.Listen(/*MAXCONNECTIONS*/);

            tcpConnections = new List<TCPConnection>();
            clientIDs = new List<int>();
            maxClientID = -1;

            serverState = new State();

            started = DateTime.UtcNow.Ticks;
            Console.WriteLine("Server started at " + started+".");
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
                    tcpConnections.Add(new TCPConnection(clientSocket));
                    clientIDs.Add(-1);
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

        public void Update()
        {
            for (int i = 0; i < tcpConnections.Count; i++)
                while (tcpConnections[i].isRecvPacket())
                {
                    SendablePacket packet = tcpConnections[i].RecvPacket();
                    ReceivePacket(packet,i);
                }
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

        // Send functions
        private void SendSyncPacket(int index)
        {
            /*
            *
            */
            HeaderPacket header = new HeaderPacket(0);
            SyncPacket sync = new SyncPacket(started);
            SendablePacket packet = new SendablePacket(header, Packet.Serialise<SyncPacket>(sync));
            tcpConnections[index].SendPacket(packet);
        }

        private void SendIDPacket(int clientID, int index)
        {
            if (index < 0 || index >= tcpConnections.Count)
                return;
            HeaderPacket header = new HeaderPacket(1);
            IDPacket id = new IDPacket(clientID);
            SendablePacket packet = new SendablePacket(header, Packet.Serialise<IDPacket>(id));
            tcpConnections[index].SendPacket(packet);
        }

        private void SendSubmarinePacket(int clientID, float gas, float brakes, float steer, float a, float u, float x, float y, float theta, long t0, int index)
        {
            if (index < 0 || index >= tcpConnections.Count)
                return;
            HeaderPacket header = new HeaderPacket(2);
            SubmarinePacket submarine = new SubmarinePacket(clientID, gas, brakes, steer, a, u, x, y, theta, t0);
            SendablePacket packet = new SendablePacket(header, Packet.Serialise<SubmarinePacket>(submarine));
            tcpConnections[index].SendPacket(packet);
        }

        // Receive functions
        private void ReceivePacket(SendablePacket packet, int index)
        {
            switch (packet.header.bodyID)
            {
                case 0:
                    SyncPacket syncPacket = Packet.Deserialise<SyncPacket>(packet.serialisedBody);
                    ReceiveSyncPacket(syncPacket, index);
                    break;
                case 1:
                    IDPacket idPacket = Packet.Deserialise<IDPacket>(packet.serialisedBody);
                    ReceiveIDPacket(idPacket, index);
                    break;
                case 2:
                    SubmarinePacket submarinePacket = Packet.Deserialise<SubmarinePacket>(packet.serialisedBody);
                    ReceiveSubmarinePacket(submarinePacket, index);
                    break;
            }
        }

        private void ReceiveSyncPacket(SyncPacket packet, int index)
        {
            // FIXME: Add delays to connections!
            //tcpConnections[index].delay = (DateTime.UtcNow.Ticks - packet.sync)/2;
            //Console.WriteLine(tcpConnections[index].delay);
        }

        private void ReceiveIDPacket(IDPacket packet, int index)
        {
            bool newClient = packet.clientID < 0;
            bool newConnection = clientIDs[index] == -1;

            // If our client is new, assign a unique clientID
            clientIDs[index] = (!newClient) ? packet.clientID : ++maxClientID;

            long t0 = DateTime.Now.Ticks;
            Console.WriteLine(t0);

            if (newClient)
            {
                // Sync, send the new client their unique clientID
                SendSyncPacket(index);
                SendIDPacket(clientIDs[index], index);

                // Create the client's submarine in the master serverState
                serverState.UpdateSubmarine(clientIDs[index], 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, t0);
            }

            if (newConnection) // Note that newConnection => newClient
            {
                // Send newly-connected clients details on all nearby submarines, including their own 
                Dictionary<int, Submarine> submarines = serverState.GetSubmarines();
                foreach (int clientID in submarines.Keys)
                    SendSubmarinePacket(clientID, submarines[clientID].gas, submarines[clientID].brakes, submarines[clientID].steer, submarines[clientID].a, submarines[clientID].u, submarines[clientID].x, submarines[clientID].y, submarines[clientID].theta, submarines[clientID].t0, index);

                // Send all other players the details of our newly-connected client
                for (int i = 0; i < tcpConnections.Count; i++)
                    if (i != index)
                        SendSubmarinePacket(clientIDs[index], 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, t0, i);
            }
        }

        private void ReceiveSubmarinePacket(SubmarinePacket packet, int index)
        {
            if (packet.clientID < 0)
                return;

            serverState.UpdateSubmarine(packet.clientID, packet.gas, packet.brakes, packet.steer, packet.a, packet.u, packet.x, packet.y, packet.theta, packet.t0);
            for (int i = 0; i < tcpConnections.Count; i++)
                if (i != index)
                    SendSubmarinePacket(packet.clientID, packet.gas, packet.brakes, packet.steer, packet.a, packet.u, packet.x, packet.y, packet.theta, packet.t0, i);
        }
    }
}
