using System;
using System.Collections.Generic;

public class State
{
    // Server Mode
    public enum Mode { lobby, match, sandbox };
    public Mode mode;

    // Lobby Variables


    // Game Variables
    public long globalStart;

    public enum Superpower { East, West, Null };
    public Dictionary<Superpower, Fleet> fleets;

    private Dictionary<int, Submarine> submarines;

    private Random rng;

    public State(int init_seed)
    {
        // Server Mode
        mode = Mode.lobby;

        // Lobby Variables

        // Game Variables
        globalStart = DateTime.UtcNow.Ticks; // Need to work with... time offsets on different devices...

        fleets = new Dictionary<Superpower, Fleet>();

        rng = new Random(init_seed);
    }

    // Switching Server Mode
    public void StartLobby()
    {

    }

    public void StartMatch(List<int> init_clientIDs, List<string> init_clientIPs)
    {
        // STEP 0: Set 'start of game'
        globalStart = DateTime.UtcNow.Ticks;

        // STEP 1: Set player roles
        List<int> order = Enumerable.Range(0, init_clientIDs.Count).OrderBy(x => rng.Next()).ToList();

        List<int> clientIDs = new List<int>();
        foreach (int index in order)
            clientIDs.Add(init_clientIDs[index]);

        List<string> clientIPs = new List<string>();
        foreach (int index in order)
            clientIPs.Add(init_clientIPs[index]);

        // We can safely assume clientIDs contains MIN_CONNECTIONS > 4 clients, so...
        fleets.Add(Superpower.East, new Fleet(clientIDs[0], clientIPs[0]));
        fleets.Add(Superpower.West, new Fleet(clientIDs[1], clientIPs[1]));

        fleets[Superpower.East].AddSubmarine(2, clientIDs[2], clientIPs[2], true); // FIXME: Fleet.AddSubmarine is *obviously* a big one...
        fleets[Superpower.West].AddSubmarine(3, clientIDs[3], clientIPs[3], true);

        for (int i = 4; i < clientIDs.Count; i++)
            fleets[(rng.Next() % 2 == 0) ? Superpower.East : Superpower.West].AddSubmarine(i, clientIDs[i], clientIPs[i], false);

        // STEP 2: Set game state
        // i.e. Submarine positions... superpower codes? (Code as covert signals to one another?)
        foreach (Superpower superpower in fleets.Keys)
            foreach (int submarineID in fleets[superpower].submarines.Keys)
                fleets[superpower].submarines[submarineID].InitialisePosition(0.0f,0.0f,0.0f,globalStart);
    }

    public void StartSandbox(List<int> init_clientIDs, List<string> init_clientIPs)
    {
        // STEP 0: Set 'start of game'
        globalStart = DateTime.UtcNow.Ticks;

        // STEP 1: Set player roles
        List<int> clientIDs = new List<int>(init_clientIDs);
        List<string> clientIPs = new List<string>(init_clientIPs);

        fleets.Add(Superpower.East, new Fleet(clientIDs[0], clientIPs[0]));

        for (int i = 4; i < clientIDs.Count; i++)
            fleets[Superpower.East].AddSubmarine(i, clientIDs[i], clientIPs[i], false);

        // STEP 2: Set game state
        // i.e. Submarine positions... superpower codes? (Code as covert signals to one another?)
        foreach (Superpower superpower in fleets.Keys)
            foreach (int submarineID in fleets[superpower].submarines.Keys)
                fleets[superpower].submarines[submarineID].InitialisePosition(0.0f, 0.0f, 0.0f, globalStart);
    }


    public Dictionary<int, Submarine> GetSubmarines()
    {
        return submarines;
    }

    public void UpdateSubmarine(int submarineID, float x, float y, float theta, long timestamp)
    {
        // FIXME: Maybe needs to be more 'in range'?
        Superpower superpower = GetSubmarineSuperpower(submarineID);
        if (superpower != Superpower.Null)
        {
            if (fleets[superpower].submarines[submarineID].UpdatePosition(x, y, theta, timestamp))
                submarines[submarineID].UpdatePredictionModel(); // Only updates prediction model if position has changed...
        }
    }

    private Superpower GetSubmarineSuperpower(int submarineID)
    {
        foreach (Superpower superpower in fleets.Keys)
            if (fleets[superpower].submarines.ContainsKey(submarineID))
                return superpower;

        return Superpower.Null;
    }
}

