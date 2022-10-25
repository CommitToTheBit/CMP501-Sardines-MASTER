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

    public void UpdateSubmarine(int clientID, float x, float y)
    {
        if (submarines.ContainsKey(clientID))
            submarines[clientID].Update(x, y);
        else
            submarines.Add(clientID, new Submarine(x, y));
        
        foreach (int key in submarines.Keys)
        {
            GD.Print(key);
            GD.Print(submarines[key].GetX());
            GD.Print(submarines[key].GetY());
        }
    }

    public void MoveSubmarine(int clientID, float deltaX, float deltaY)
    {
        if (submarines.ContainsKey(clientID))
            submarines[clientID].Update(submarines[clientID].GetX()+deltaX, submarines[clientID].GetY()+deltaY);
    }
}
