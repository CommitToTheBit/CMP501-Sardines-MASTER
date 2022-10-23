using Godot;
using System;
using System.Collections.Generic;

public class Player
{   
    public int superpower = 0; // 0 for NULL, 1 for EAST, 2 for WEST
    public int order = 0; // 0 for HOLD, 1 for PRIME, 2 for LAUNCH
    public int armament = 0; // 0 for NOT_PRIMED, 1 for PRIMED, 2 for LAUNCHING, -1 for LAUNCHED
}

public class Diplomat : Player
{
protected
    Dictionary<string,int> superpowers = new Dictionary<string,int>();
    Dictionary<string,int> orders = new Dictionary<string,int>();
    Dictionary<string,int> armaments = new Dictionary<string,int>();
}

public class Crew : Player
{
    protected string submarine = "A1234";

}
