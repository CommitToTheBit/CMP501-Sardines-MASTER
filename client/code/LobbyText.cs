using Godot;
using System;
using System.Collections.Generic;

public class LobbyText : Text
{
    Handler handler;
    List<RichTextLabel> players;

    public override void _Ready()
    {
        handler = GetNode<Handler>("/root/Handler");
        handler.client.Connect("ReceivedPacket",this,"Receive");

        InitialiseText();
        GetNode<TextureButton>("StartGameButton").GrabFocus();
        UpdateHost();

        players = new List<RichTextLabel>();
        for (int i = 0; i < 8; i++)
        {
            string column = (i%2 == 0) ? "West" : "East";
            players.Add(GetNode<RichTextLabel>("Players/Players"+column+"/Player #"+(i+1)));
        }
        UpdatePlayers();
    }

    public void StartGamePressed()
    {
        handler.client.Send2310();
    }

    public void BackPressed()
    {
        string backID = history[history.Count-1];
        List<string> newHistory = new List<string>(history);
        newHistory.RemoveAt(history.Count-1);

        EmitSignal("ChangeUI",backID,backID,newHistory);
    }

    public void UpdateHost() // FIXME: Trivial for now, will add 'proper' host functionality later...
    {
        bool host = true;
        if (!host && GetNode<TextureButton>("StartGameButton").HasFocus())
            GetNode<TextureButton>("BackButton").GrabFocus();
        GetNode<TextureButton>("StartGameButton").Disabled = !host; 
    }

    public void UpdatePlayers()
    {
        List<string> alphabet = new List<string> {"A","B","C","D","E","F","G","H","I","J","K","L","M","N","O","P","Q","R","S","T","U","V","W","X","Y","Z"};
        List<string> digits = new List<string> {"0","1","2","3","4","5","6","7","8","9"};

        // STEP 1: Show self in lobby...
        players[0].BbcodeText = "[b]001: [/b]"+alphabet[handler.client.GetClientID()%alphabet.Count]+"-001";

        // STEP 2: Show other players in lobby...
        List<int> keys = handler.client.GetClientIDs();
        for (int i = 1; i < Mathf.Min(keys.Count+1, players.Count); i++)
        {
            // Constructing names for other players, based on ip...
            char[] ip = handler.client.GetClientIP(keys[i-1]).Split(".")[3].ToCharArray();
            string name = "";
            foreach (char digit in ip)
                if (digits.Contains(digit.ToString()))
                    name += digit;
            while (name.Length() < 3)
                name = "0"+name;
            name = alphabet[keys[i-1]%alphabet.Count]+"-"+name;

            // Update RichTextLabel to contain player's name...
            players[i].BbcodeText = "[b]00"+(i+1)+": [/b]"+name;
        }

        // STEP 3: Show vacancies in lobby...
        for (int i = keys.Count+1; i < players.Count; i++)
        {
            if (i == keys.Count+1)
                players[i].BbcodeText = "[b]00"+(i+1)+": [/b][color=#646e76][i]M.I.A.?[/i][/color]";
            else
                players[i].BbcodeText = "[b]00"+(i+1)+":";
        }
    }

    public void Receive(int packetID)
    {
        switch (packetID)
        {
            case 1002:
                UpdatePlayers();
                return;

            case 1003:
                UpdatePlayers();
                return;

            case 2311:
                List<string> newHistory = new List<string>(history);
                newHistory.Add(id);

                // FIXME: Run for loop/function to find client role (from handler.client.state)
                EmitSignal("ChangeUI","Navigation","Navigation",newHistory);
                return;
        }
    }
}
