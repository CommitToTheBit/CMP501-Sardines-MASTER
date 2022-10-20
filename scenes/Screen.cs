using Godot;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class Screen : Control
{
    // Declare member variables here. Examples:
    // private int a = 2;
    // private string b = "text";

    // Called when the node enters the scene tree for the first time.
    const int MESSAGESIZE = 40;

    Socket socket;
    Label messenger;

    int counter = 0;

    public override void _Ready()
    {
        IPAddress ipAddress = IPAddress.Parse("127.0.0.1");

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
        }

        messenger = GetNode<Label>("Messenger");
    }

    public override void _Process(float delta)
    {

    }

    public override void _Input(InputEvent @event)
    {
        if (Input.IsActionJustPressed("ui_accept"))
        {
            string message = messenger.Text.PadRight(MESSAGESIZE,'#');

            byte[] buffer = Encoding.UTF8.GetBytes(message);

            socket.Send(buffer,0,MESSAGESIZE,0);
            socket.Receive(buffer,0,MESSAGESIZE,0);

            messenger.Text = Encoding.UTF8.GetString(buffer).Split("#")[0];

            /*counter++;

            NetworkStream ns = tcpClient.GetStream();

            byte[] messageBytesToSend = new byte[128];
            string messageToSend = "messenger.Text"+"###";
            while (messageToSend.Length() < 40)
                messageToSend += " ";   
            messageBytesToSend = Encoding.UTF8.GetBytes(messenger.Text);
            GD.Print(messageBytesToSend);
            ns.Write(messageBytesToSend, 0, messageBytesToSend.Length);
            byte[] messageBytesToRecv = new byte[40];
            int count = ns.Read(messageBytesToRecv, 0, messageBytesToRecv.Length);
            string messageToRecv = Encoding.UTF8.GetString(messageBytesToRecv).Split("###")[0];
            GD.Print(messageToRecv);
            messenger.Text = Encoding.UTF8.GetString(messageBytesToRecv);*/
        }
    }

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }
}
