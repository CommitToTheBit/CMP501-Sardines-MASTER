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

    public void UpdateSubmarine(int clientID, float x, float y, float direction, float steer)
    {
        if (submarines.ContainsKey(clientID))
            submarines[clientID].Update(x, y, direction, steer);
        else
            submarines.Add(clientID, new Submarine(x, y, direction, steer));
    }

    public void MoveSubmarine(int clientID, float deltaX, float deltaY, float deltaDirection, float deltaSteer)
    {
        if (submarines.ContainsKey(clientID))
            submarines[clientID].Update(submarines[clientID].GetX()+deltaX, submarines[clientID].GetY()+deltaY, submarines[clientID].GetDirection()+deltaDirection, submarines[clientID].GetSteer()+deltaSteer);
    }
}
