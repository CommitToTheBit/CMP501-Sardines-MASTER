using Godot;
using System;

public class PseudoButton : Button
{
    [Export] public string unencodedText;

    private RichTextLabel pseudoText;
    
    public override void _Ready()
    {
        pseudoText = GetNode<RichTextLabel>("PseudoText");
        SetBbcode(false);
    }

    public void SetBbcode(bool focus)
    {
        string indent = ">  ";
        string bold = "[b]";
        string unbold = "[/b]";
        string visible = "[color=#e8f0f0]";
        string invisible = "[/color]";

        if (focus)
            pseudoText.BbcodeText = visible+bold+indent+unencodedText+unbold+invisible;
        else
            pseudoText.BbcodeText = bold+indent+unbold+visible+unencodedText+invisible;
    }
}
