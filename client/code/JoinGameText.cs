using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

public class JoinGameText : Text
{
    private Handler handler;

    private PseudoButton ipPseudoButton;
    private string ip;
    private string underscore;
    private Timer timer;

    public override void _Ready()
    {
        handler = GetNode<Handler>("/root/Handler");
        handler.client.Connect("ReceivedPacket",this,"Receive");

        InitialiseText();
        ipPseudoButton = GetNode<PseudoButton>("IPPseudoButton");

        timer = new Timer();
        timer.WaitTime = 0.72f;
        timer.Autostart = false;
        timer.OneShot = false;
        AddChild(timer);

        ip = "";
        underscore = "_";

        ipPseudoButton.Connect("focus_entered",timer,"start");
        ipPseudoButton.Connect("focus_exited",timer,"stop");
        ipPseudoButton.Connect("focus_entered",this,"SetUnderscore",new Godot.Collections.Array() {"_"});
        ipPseudoButton.Connect("focus_exited",this,"SetUnderscore",new Godot.Collections.Array() {""});
        timer.Connect("timeout",this,"SetUnderscore",new Godot.Collections.Array() {"!"});
        
        ipPseudoButton.GrabFocus();
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if (!ipPseudoButton.HasFocus())
            return;
        
        if (!(@event is InputEventKey) || !@event.IsPressed())
            return;

        string digit = OS.GetScancodeString(((InputEventKey)@event).Scancode);
        if (digit.Equals("Period"))
            digit = ".";

        List<string> ipDigits = new List<string>() { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "."};
        if (ipDigits.Contains(digit) && ip.Length < 15)
        {
            ip += digit;
            SetIP();
        }
        else if ("BackSpace".Equals(digit) && ip.Length > 0)
        {
            ip = ip.Substring(0,ip.Length-1);
            SetIP();
        }
    }

    public void SetIP()
    {
        ipPseudoButton.unencodedText = "IP  "+ip;
        if (ip.Length < 15)
            ipPseudoButton.unencodedText += underscore;
        ipPseudoButton.FormatBbcode(ipPseudoButton.HasFocus());
    }

    public void SetUnderscore(string init_underscore)
    {
        if (init_underscore.Equals("!"))
            underscore = (underscore.Equals("_")) ? "" : "_";
        else
            underscore = init_underscore;

        SetIP();
    }

    public void IPPressed()
    {
        GD.Print("IP Pressed... "+ip+"...");

        // Handling invalid IP addresses...
        // Since IPv4 is a 32-bit number, represented in a "dotted-decimal format", we can immediately rule out any typos...
        // (While shorthands that do not follow this format exist - i.e. "1" represents "127.0.0.1", our server will always give its full IP)
        try
        {
            List<int> ipDecimals = new List<string>(ip.Split(".")).Select(int.Parse).ToList();

            if (ipDecimals.Count != 4)
                throw new Exception("Incorrect number of decimals...");

            foreach (int ipDecimal in ipDecimals)
                if (ipDecimal >= 256)
                    throw new Exception("Maximum 8 bits per decimal...");
        }
        catch
        {
            GD.Print("Invalid IP...");
            return;
        }

        // FIXME: Set up a timeout on connection? Play a spinning wheel while doing so?
        if (!handler.client.Connect(ip))
        {

        }

        // If we have successfully connected to the server, then our first 1000 packet has been sent...
        // We now wait to receive a packet that will tell us what scene to switch to...
    }

    public void BackPressed()
    {
        string backID = history[history.Count-1];
        List<string> newHistory = new List<string>(history);
        newHistory.RemoveAt(history.Count-1);

        EmitSignal("ChangeUI",backID,backID,newHistory);
    }

    public void Receive(int packetID)
    {
        switch (packetID)
        {
            case 1201:
                List<string> newHistory = new List<string>(history);
                newHistory.Add(id);

                EmitSignal("ChangeUI","Lobby","Lobby",newHistory);
                return;
        }
        // NOTE: There will be other cases here!
    }
}
