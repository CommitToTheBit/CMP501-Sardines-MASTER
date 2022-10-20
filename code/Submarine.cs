using Godot;
using System;
using System.Collections.Generic;

class Submarine 
{
private

    // Diplomatic Variables
    enum Superpower { EAST, WEST, NULL };
    Superpower superpower = Superpower.NULL;

    enum Orders { HOLD, PRIME, LAUNCH }
    Orders orders = Orders.PRIME;

    // Command Variables
    string id = "A1234";
    List<string> log = new List<string>();

    enum Armament { NPRIMED, PRIMED, LAUNCHING, LAUNCHED, NULL };
    Armament armament = Armament.PRIMED;
    int countdown = 10;

    // Navigation Variables
    Vector2 globalCoordinates = new Vector2(0.0f,0.0f);

    // Communications Variables


}
