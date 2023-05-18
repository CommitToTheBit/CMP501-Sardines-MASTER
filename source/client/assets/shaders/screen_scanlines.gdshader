shader_type canvas_item;

// Scanline parameters
uniform float scanlines = 80;

// Constants
const float PI = 3.14159265359;

void fragment()
{
	float s = cos(scanlines*PI*UV.y);
	COLOR = max(texture(TEXTURE,UV),vec4(0.02*vec3(s),1.0));
}