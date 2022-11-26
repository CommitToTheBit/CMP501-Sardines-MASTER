shader_type canvas_item;

// CRT parameters
uniform vec3 crt_dark = vec3(0.01,0.02,0.02);
uniform vec3 crt_light = vec3(0.1,0.8,0.4);
uniform float crt_falloff_exponent = 0.5;

// Bloom parameters
uniform float bloom_radius = 1.0;
uniform float bloom_normalisation = 0.4;

// Constants
const float PI = 3.14159265359;

vec4 get_crt_interpolation(vec4 c)
{
	float r = (1.0-c.r)*crt_dark.r+c.r*crt_light.r;
	float g = (1.0-c.g)*crt_dark.g+c.g*crt_light.g;
	float b = (1.0-c.b)*crt_dark.b+c.b*crt_light.b;
	return vec4(r,g,b,c.a);
}

vec2 get_crt_coordinates(vec2 uv)
{
	//return uv;
	// Centering UV coordinates
	uv = 2.0*(uv-vec2(660.0/1920.0,540.0/1080.0));
	uv.x *= 1920.0/960.0;
	uv.y *= 1080.0/720.0;
	// Equation of CRT monitor
	uv.x *= 0.9 + pow(abs(uv.y) / 3.0, 2.0);
	uv.y *= 0.9 + pow(abs(uv.x) / 3.0, 2.0);
	// Undoing centering of UV coordinates
	uv.x /= 1920.0/960.0;
	uv.y /= 1080.0/720.0;
	uv = 0.5*uv+vec2(660.0/1920.0,540.0/1080.0);
	return uv;
}

float get_crt_falloff(vec2 uv)
{
	// Centering UV coordinates
	uv = 2.0*(uv-vec2(660.0/1920.0,540.0/1080.0));
	uv.x *= 1920.0/960.0;
	uv.y *= 1080.0/720.0;
	//
	if (abs(uv.x) < 1.0 && abs(uv.y) < 1.0)
		return 1.0; 
	if (abs(uv.x) > abs(uv.y))
		return 1.0/pow(abs(1.01*uv.x),16.0);
	else
		return 1.0/pow(abs(1.01*uv.y),16.0);
}

vec3 get_pixel_bloom(sampler2D tex, vec2 uv) 
{
	float falloff = get_crt_falloff(uv);
	if (falloff < 1.0)
		return vec3((0.01+0.125*0.09)*falloff,(0.02+0.125*0.78)*falloff,(0.02+0.125*0.38)*falloff);
	return max(textureLod(tex,uv,1.0).rgb-0.01,vec3(0.0));
}

vec3 get_coordinate_bloom(sampler2D tex, vec2 uv, vec2 ps)
{
	// Scale pixel size by bloom_radius
	ps *= bloom_radius;
	// Define bloom colour
	vec3 bloom = vec3(0.0);
	// Add 16 inner points to bloom
	for (float i = 0.0; i < 8.0; i++)
	{
		bloom += bloom_normalisation*get_pixel_bloom(tex,uv+vec2(ps.x*cos((i/4.0)*PI),ps.y*sin((i/4.0)*PI)));
		bloom += pow(bloom_normalisation,2.0)*get_pixel_bloom(tex,uv+2.0*vec2(ps.x*cos((i/4.0)*PI),ps.y*sin((i/4.0)*PI)));
	}
	return bloom;
}

void fragment()
{
	// Map screen coordinates to CRT coordinates
	vec2 uv = get_crt_coordinates(SCREEN_UV);
	// Define colour
	vec4 colour;
	if (uv.x > 184.0/1920.0 && uv.x < 1136.0/1920.0 && uv.y > 184.0/1080.0 && uv.y < 896.0/1080.0)
		// If point lies 'on' the CRT screen, return it unchanged
		colour = texture(SCREEN_TEXTURE,uv);
	else
		// Otherwise, calculate distance (and therefore glow) from CRT monitor
		colour = vec4(0.0,0.0,0.0,1.0);//vec4(vec3(get_crt_falloff(uv)),1.0);
	// Add bloom to screen
	vec3 bloom = get_coordinate_bloom(SCREEN_TEXTURE,uv,SCREEN_PIXEL_SIZE); 
	colour.rgb += bloom;
	COLOR = colour;
}