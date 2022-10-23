using Godot;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class Screen : Control
{
    private TCPConnection tcpConnection;
    private bool connected;

    const int MESSAGESIZE = 40;

    Socket socket;
    Label messenger;

    int counter = 0;

    public override void _Ready()
    {
        tcpConnection = new TCPConnection();
        connected = false;

        messenger = GetNode<Label>("Messenger");
    }

    public override void _Process(float delta)
    {
        if (connected)
        {

        }
        else
        {
            connected = tcpConnection.Connect();
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
}
