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
        timer.WaitTime = 0.3f;
        timer.Autostart = false;
        timer.OneShot = false;
        AddChild(timer);

        underscore = "_";
        ipPseudoButton.Connect("focus_entered",timer,"start");
        ipPseudoButton.Connect("focus_exited",timer,"stop");
        ipPseudoButton.Connect("focus_entered",this,"SetUnderscore",new Godot.Collections.Array() {"_"});
        ipPseudoButton.Connect("focus_exited",this,"SetUnderscore",new Godot.Collections.Array() {""});
        timer.Connect("timeout",this,"SetUnderscore",new Godot.Collections.Array() {((underscore.Equals("_")) ? "" : "_")/**/});
        
        ipPseudoButton.GrabFocus();//GetNode<RichTextLabel>("InternetworkPseudoButton").GrabFocus();
    }

    public void SetIP()
    {
        ipPseudoButton.unencodedText = "IP "+ip+underscore;
        ipPseudoButton.FormatBbcode(ipPseudoButton.HasFocus());
    }

    public void SetUnderscore(string init_underscore)
    {
        GD.Print(underscore+" to "+init_underscore+" "+DateTime.UtcNow.Ticks);
        underscore = init_underscore;
        SetIP();
    }

    public void IPPressed()
    {
        GD.Print("IP Pressed...");
    }

    public void BackPressed()
    {
        string backID = history[history.Count-1];
        List<string> newHistory = new List<string>(history);
        newHistory.RemoveAt(history.Count-1);

        EmitSignal("ChangeUI",backID,backID,newHistory);
    }
}
