using System;
using System.Collections.Generic;

public class State
{
    // Server Mode
    public enum Mode { lobby, match, sandbox };
    public Mode mode;

    // Lobby Variables


    // Game Variables
    public enum Superpower { East, West };
    public Dictionary<Superpower, Fleet> fleets;

    private Dictionary<int, Submarine> submarines;

    private Random rng;

    public State(int init_seed)
    {
        // Server Mode
        mode = Mode.lobby;

        // Lobby Variables

        // Game Variables
        fleets = new Dictionary<Superpower, Fleet>();

        submarines = new Dictionary<int, Submarine>();

        rng = new Random(init_seed);
    }

    // Switching Server Mode
    public void ServerStartLobby()
    {

    }

    public void ServerStartMatch(List<int> init_clientIDs)
    {

        // STEP 1: Set player roles
        List<int> clientIDs = init_clientIDs.OrderBy(x => rng.Next()).ToList(); // FIXME: May wish to integrate override for testing purposes?

        // We can safely assume clientIDs contains MIN_CONNECTIONS > 4 clients, so...
        fleets.Add(Superpower.East, new Fleet(clientIDs[0]));
        fleets.Add(Superpower.West, new Fleet(clientIDs[1]));

        fleets[Superpower.East].AddSubmarine(clientIDs[2]); // FIXME: Fleet.AddSubmarine is *obviously* a big one...
        fleets[Superpower.West].AddSubmarine(clientIDs[3]);

        for (int i = 4; i < clientIDs.Count; i++)
            fleets[(rng.Next() % 2 == 0) ? Superpower.East : Superpower.West].AddSubmarine(clientIDs[i]);

        // STEP 2: Set game state
        // i.e. Submarine positions... superpower codes? (Code as covert signals to one another?)
    }


    public Dictionary<int, Submarine> GetSubmarines()
    {
        return submarines;
    }

    public void UpdateSubmarine(int clientID, float x, float y, float theta, long timestamp)
    {
        if (submarines.ContainsKey(clientID))
        {
            if (submarines[clientID].UpdatePosition(x, y, theta, timestamp))
                submarines[clientID].UpdatePredictionModel(); // Only updates prediction model if position has changed...
        }
        else
        {
            submarines.Add(clientID, new Submarine(x, y, theta, timestamp));
        }
    }
}

