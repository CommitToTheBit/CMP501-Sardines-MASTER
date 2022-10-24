using Godot;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class Screen : Control
{
    private TCPConnection tcpConnection;
    private Timer positionTimer;
    private bool connected;

    const int MESSAGESIZE = 40;


    Label messenger;

    int counter = 0;

    public override void _Ready()
    {
        tcpConnection = new TCPConnection();
        connected = false;

        messenger = GetNode<Label>("Messenger");

        positionTimer = new Timer();
        positionTimer.WaitTime = 1.0f;
        positionTimer.Autostart = false;
        positionTimer.OneShot = false;
        AddChild(positionTimer);
        positionTimer.Connect("timeout",this,"SendPositionPacket");

    }

    public override void _Process(float delta)
    {
        if (connected)
        {

        }
        else
        {
            connected = tcpConnection.Connect();
            positionTimer.Start();
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (Input.IsActionJustPressed("ui_accept"))
        {
            tcpConnection.SetBuffer(messenger.Text.PadRight(MESSAGESIZE,'#'));

            tcpConnection.WriteFromBuffer();
            tcpConnection.ReadToBuffer();

            messenger.Text = tcpConnection.GetBuffer();
        }
    }

    public void SendPositionPacket()
    {
        GD.Print("timer running!");
        tcpConnection.SerialisePositionPacket();
    }
}
