using Godot;
using System;

public class Handler : Node
{
    public Client client; // Allows for disconnects?

    public override void _Ready()
    {
        client = new Client();
    }

    //public void 

    public override void _Process(float delta)
    {
        if (client.IsConnected())
        {
            client.Write();
            client.Read();
            client.Update();
        }
        else
        {
            //c.Connect();
        }
    }

    public void Update()
    {
        
    }
}
