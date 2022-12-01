using System;
using System.Collections.Generic;

namespace server
{
    public class State
    {
        // Server Mode
        public enum Mode { lobby, match };
        public Mode mode;

        // Lobby Variables


        // Game Variables
        public enum Superpower { East, West };
        public Dictionary<Superpower, Fleet> fleets;

        private Dictionary<int, Submarine> submarines;

        public State()
        {
            // Server Mode
            mode = Mode.lobby;

            // Lobby Variables

            // Game Variables
            fleets = new Dictionary<Superpower, Fleet>();

            submarines = new Dictionary<int, Submarine>();
        }

        // Switching Server Mode
        public void StartLobby()
        {

        }

        public void StartMatch()
        {

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
}

public class Fleet
{

}

public class Diplomat
{

}

public class Submarine
{

}

public class Crew
{

}

public class Captain : Crew
{

}