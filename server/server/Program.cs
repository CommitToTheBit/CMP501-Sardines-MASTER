using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine(Packet.GetSize(1001));
        /*Server server = new Server();

        while (true)
        {
            server.Read();
            server.Update();
            server.Write();
        }*/
    }
}