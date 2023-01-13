shader_type canvas_item;

uniform float theta_range = 45.0;

uniform float alpha = 1.0f;

// Constants
const float PI = 3.14159265359;

void fragment()
{
	vec2 uv = 2.0*UV-vec2(1.0,1.0);
	float r = length(uv);
	float theta = atan(-uv.x,-uv.y);
	vec4 colour = texture(SCREEN_TEXTURE,SCREEN_UV);
	
	if (r < 1.0)
	{
		if (theta < 0.5*PI*(theta_range+5.0)/180.0 && theta > -0.5*PI*(theta_range+5.0)/180.0)
			colour = vec4(1.0,1.0,1.0,0.625*(1.0-r*r));
		if (theta < 0.5*PI*theta_range/180.0 && theta > -0.5*PI*theta_range/180.0)
			colour = vec4(1.0,1.0,1.0,0.625*(1.0-r));
	}
	
	COLOR = colour;
}