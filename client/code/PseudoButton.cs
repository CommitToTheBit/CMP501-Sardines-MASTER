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

    public void SetBbcode(bool isFocused)
    {
        string indent = ">  ";
        string bold = "[b]";
        string unbold = "[/b]";
        string visible = "[color=#ffffff]";
        string invisible = "[/color]";

        if (isFocused)
            pseudoText.BbcodeText = visible+bold+indent+unencodedText+unbold+invisible;
        else
            pseudoText.BbcodeText = bold+indent+unbold+visible+unencodedText+invisible;
    }
}
