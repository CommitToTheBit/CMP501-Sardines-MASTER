using Godot;
using System;
using System.Collections.Generic;

public class JoinGameText : Text
{
    private PseudoButton ipPseudoButton;
    private string ip;
    private string underscore;
    private Timer timer;

    public override void _Ready()
    {
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
    }

    public void BackPressed()
    {
        string backID = history[history.Count-1];
        List<string> newHistory = new List<string>(history);
        newHistory.RemoveAt(history.Count-1);

        EmitSignal("ChangeUI",backID,backID,newHistory);
    }
}
