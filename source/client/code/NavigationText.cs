using Godot;
using System;

public class NavigationText : Text
{
    public override void _Ready()
    {
        InitialiseText();

        GetParent().GetParent().GetNode<NavigationDisplay>("Monitor/Display/NavigationDisplay").Show();
    }
}
