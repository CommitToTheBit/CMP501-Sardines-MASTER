using Godot;
using System;
using System.Collections.Generic;

public class Soundwave : Node2D
{
    [Signal] delegate void WaveReceivedBy(int submarineID, long intervalTicks);

    ColorRect arc;
    
    Area2D outerArea;
    CollisionShape2D outerHitbox;

    Area2D innerArea;
    CollisionShape2D innerHitbox;

    ShaderMaterial shaderMaterial;
    Tween tween;

    List<Area2D> collisions;

    long init_ticks;

    public override void _Ready()
    {
        arc = GetNode<ColorRect>("Arc");

        outerArea = GetNode<Area2D>("OuterArea");
        outerHitbox = outerArea.GetNode<CollisionShape2D>("OuterHitbox");

        innerArea = GetNode<Area2D>("InnerArea");
        innerHitbox = innerArea.GetNode<CollisionShape2D>("InnerHitbox");

        shaderMaterial = (arc.Material as ShaderMaterial);

        tween = GetNode<Tween>("Tween");

        collisions = new List<Area2D>();
        collisions.Add(innerArea);
    }

    public async void PropagateWave(float r_initial, float r_range, float r_width, float theta_range, float period, bool collision)
    {
        r_initial = Mathf.Min(r_initial,r_width);

        // Set canvas...
        arc.RectPosition = -r_range*Vector2.One;
        arc.RectMinSize = 2.0f*r_range*Vector2.One;
        arc.RectSize = 2.0f*r_range*Vector2.One;

        // Set collision constants...
        outerHitbox.Disabled = !collision;
        innerHitbox.Disabled = !collision;

        // Set collision tween...
        tween.InterpolateProperty(outerHitbox.Shape,"radius",r_initial,r_range,period);
        tween.InterpolateProperty(innerHitbox.Shape,"radius",r_initial-r_width,r_range-r_width,period);

        // Set shader constants...
        shaderMaterial.SetShaderParam("r_range", r_range);
        shaderMaterial.SetShaderParam("r_width", r_width);
        shaderMaterial.SetShaderParam("theta_range", theta_range);

        // Set shader tween...
        tween.InterpolateMethod(this,"ShadeWave",r_initial,r_range,period);
        tween.InterpolateMethod(this,"FadeWave",0.625f,0.0f,period);

        // Propagate wave...
        outerArea.Connect("area_entered",this,"ReceiveWave");

        init_ticks = DateTime.UtcNow.Ticks;
        tween.Start();

        // Delete on completion...
        await ToSignal(tween,"tween_all_completed");
        QueueFree();
    }

    public void ShadeWave(float r)
    {
        shaderMaterial.SetShaderParam("r_outer", r);
    }

    public void FadeWave(float alpha)
    {
        shaderMaterial.SetShaderParam("alpha", alpha);
    }

    public void ReceiveWave(Area2D receiver)
    {
        if (collisions.Contains(receiver))
            return;

        long intervalTicks = DateTime.UtcNow.Ticks-init_ticks;
        EmitSignal("WaveReceivedBy",receiver.GetParent<Vessel>().submarineID,intervalTicks); // Track submarineID, etc...

        collisions.Add(receiver);
    }
}
