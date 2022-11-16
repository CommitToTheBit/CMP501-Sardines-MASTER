using Godot;
using System;

public class Handler : Node
{
    public Client c; // Allows for disconnects?

    public override void _Ready()
    {
        c = new Client();
    }

    //public void 

    public override void _Process(float delta)
    {
        if (c.IsConnected())
        {
            c.Write();
            c.Read();
            c.Update();
        }
        else
        {
            c.Connect();
        }
    }
}
