using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;

namespace server
{
    class Program
    {
        static void Main(string[] args)
        {

            Server server = new Server();

            while (true)
            {
                server.Read();
                server.Update();
                server.Write();
            }
            //IPHostEntry host = Dns.GetHostEntry("localhost");
            /*IPAddress ipAddress = IPAddress.Parse("127.0.0.1");//host.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress,5555);

            Socket serverSocket = new Socket(ipAddress.AddressFamily,SocketType.Stream,ProtocolType.Tcp);
            serverSocket.Bind(localEndPoint);
            serverSocket.Listen(/*MAXCONNECTIONS*//*);

            List<TCPConnection> conns = new List<TCPConnection>();

            while (true)
            {
                // The structure that describes the set of sockets we're interested in reading from.
                List<Socket> readable = new List<Socket>();

                // The structure that describes the set of sockets we're interested in writing to.
                List<Socket> writeable = new List<Socket>();

                readable.Add(serverSocket);

                foreach (TCPConnection conn in conns)
                {
                    if (conn.IsRead())
                    {
                        readable.Add(conn.GetSocket());
                    }
                    if (conn.IsWrite())
                    {
                        writeable.Add(conn.GetSocket());
                    }
                }

                Socket.Select(readable, writeable, null, 1000000);
                Console.WriteLine(conns.Count() + " clients, " + (readable.Count() + writeable.Count()) + " are ready.");

                if (readable.Contains(serverSocket))
                {
                    try
                    {
                        Socket clientSocket = serverSocket.Accept();
                        conns.Add(new TCPConnection(clientSocket));
                    }
                    catch
                    {

                    }
                }

                // Check each of the clients.
                for (int i = 0; i < conns.Count();)
                {
                    bool dead = false;
                    if (readable.Contains(conns[i].GetSocket()))
                    {
                        dead |= conns[i].Read();
                    }
                    if (writeable.Contains(conns[i].GetSocket()))
                    {
                        dead |= conns[i].Write();
                    }
                    if (dead)
                    {
                        conns[i].GetSocket().Dispose();
                        conns.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                }

                // Handle data received from each client
            }*/
        }
    }
}