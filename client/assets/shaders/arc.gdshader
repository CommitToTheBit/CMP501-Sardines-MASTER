shader_type canvas_item;

uniform float r_max = 1440.0;
uniform float r_outer = 720.0;
uniform float r_width = 16.0;

uniform float range = 30.0;

// Constants
const float PI = 3.14159265359;

void fragment()
{
	vec2 uv = 2.0*UV-vec2(1.0,1.0);
	float r = length(uv);
	float theta = atan(-uv.x,-uv.y);
	if (r <= r_outer/r_max && r > (r_outer-r_width)/r_max && theta < PI*range/180.0 && theta > -PI*range/180.0)
		COLOR = vec4(1.0,1.0,1.0,1.0);
}