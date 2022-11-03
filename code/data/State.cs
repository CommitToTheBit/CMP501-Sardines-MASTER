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

    public void UpdateSubmarine(int clientID, float gas, float brakes, float steer, float a, float u, float x, float y, float theta, long t0)
    {
        if (submarines.ContainsKey(clientID))
            submarines[clientID] = new Submarine(gas, brakes, steer, a, u, x, y, theta, t0);
        else
            submarines.Add(clientID, new Submarine(gas, brakes, steer, a, u, x, y, theta, t0));
    }

    /*public void MoveSubmarine(int clientID, float deltaX, float deltaY, float deltaDirection, float deltaSteer)
    {
        if (submarines.ContainsKey(clientID))
            submarines[clientID].Update(submarines[clientID].GetX()+deltaX, submarines[clientID].GetY()+deltaY, submarines[clientID].GetDirection()+deltaDirection, submarines[clientID].GetSteer()+deltaSteer);
    }*/
}
