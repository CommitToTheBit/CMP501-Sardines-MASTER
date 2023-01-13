using Godot;
using System;

public class Soundwave : Node2D
{
    ColorRect arc;
    CollisionShape2D outerHitbox;
    CollisionShape2D innerHitbox;
    ShaderMaterial shaderMaterial;
    Tween tween;

    public override void _Ready()
    {
        arc = GetNode<ColorRect>("Arc");
        outerHitbox = GetNode<CollisionShape2D>("OuterArea/OuterHitbox");
        innerHitbox = GetNode<CollisionShape2D>("InnerArea/InnerHitbox");
        shaderMaterial = (arc.Material as ShaderMaterial);

        tween = GetNode<Tween>("Tween");

        PropagateWave(0.0f,1440.0f,32.0f,300.0f,2.0f,false);
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
}
