using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Fleet
{
    public int defcon;

    public Diplomat diplomat;
    public Dictionary<int, Submarine> submarines;

    // Constructor
    public Fleet(int init_clientID)
    {
        defcon = 1;

        diplomat = new Diplomat(init_clientID);
        submarines = new Dictionary<int, Submarine>();
    }

    // Functions
    public void AddSubmarine(int init_clientID)
    {
        // FIXME: Get this up and running!
    }
}

public class Diplomat
{
    public int clientID;

    public int deltaDefcon;

    public Diplomat(int init_clientID)
    {
        clientID = init_clientID;

        deltaDefcon = 1;
    }
}