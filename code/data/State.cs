using Godot;
using System;
using System.Collections.Generic;

public class State
{
    private Dictionary<int, Submarine> submarines;

    public State()
    {
        submarines = new Dictionary<int, Submarine>();
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
