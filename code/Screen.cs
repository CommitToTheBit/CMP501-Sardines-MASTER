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

        /*IPAddress ipAddress = IPAddress.Parse("127.0.0.1");

        // Create a TCP socket that we'll connect to the server
        socket = new Socket(ipAddress.AddressFamily,SocketType.Stream,ProtocolType.Tcp);

        IPAddress serverIPAddress = IPAddress.Parse("127.0.0.1");
        IPEndPoint serverEndPoint = new IPEndPoint(serverIPAddress,5555);

        // Connect the socket to the server.
        try
        {
            socket.Connect(serverEndPoint);
            GD.Print("Connected to server");
        }
        catch
        {
            GD.Print("Could not connect to server");
        }*/

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

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }
}
