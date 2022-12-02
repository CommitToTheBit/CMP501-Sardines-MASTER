using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Fleet
{
    // FIXME: Do clients need such a complicated structure?
    public Diplomat diplomat;
    public Dictionary<int, Submarine> submarines;

    public int defcon;

    // Constructor
    public Fleet(int init_clientID, string init_clientIP)
    {
        diplomat = new Diplomat(init_clientID, init_clientIP);
        submarines = new Dictionary<int, Submarine>();

        defcon = 5; // NB: This is the lowest DEFCON
    }

    // Functions
    public void AddSubmarine(int init_submarineID, int init_clientID, string init_clientIP, bool init_nuclearCapability)
    {
        submarines.Add(init_clientID, new Submarine(init_clientID, init_clientIP, init_nuclearCapability));
    }
}

public class Diplomat
{
    public int clientID;
    public string clientIP;

    public int deltaDefcon;

    public Diplomat(int init_clientID, string init_clientIP)
    {
        clientID = init_clientID;
        clientIP = init_clientIP;

        deltaDefcon = -1; // NB: Raising DEFCON by default
    }
}