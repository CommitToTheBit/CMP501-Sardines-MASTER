shader_type canvas_item;

uniform float bitdepth = 4.0;
uniform vec4 black = vec4(0.01,0.02,0.02,1.0);
uniform vec4 white = vec4(0.1,0.8,0.4,1.0);

void fragment()
{
	vec2 uv = vec2((1920.0/8.0)*UV.x,(1080.0/8.0)*UV.y);
	vec2 iuv = floor(uv);
	vec2 fuv = fract(uv);
	
	vec2 cuv = vec2((iuv.x+0.5)/(1920.0/8.0),(iuv.y+0.5)/(1080.0/8.0));
	
	vec4 colour;
	if (min(fuv.x,fuv.y) < 0.3125 || max(fuv.x,fuv.y) > 0.6875)
	{
		colour = black;
	}
	else if (UV.x >= 184.0/1920.0 && UV.x <= 1136.0/1920.0 && UV.y >= 184.0/1080.0 && UV.y <= 896.0/1080.0)
	{
		colour = texture(SCREEN_TEXTURE,vec2((iuv.x+0.5)/(1920.0/8.0),1.0-(iuv.y+0.5)/(1080.0/8.0)));
		float grayscale = floor((pow(2.0,bitdepth)-1.0)*(colour.x+colour.y+colour.z)/3.0)/(pow(2.0,bitdepth)-1.0);
		colour = (1.0-grayscale)*black+grayscale*white;
	}
	else
	{
		colour = vec4(0.0,0.0,0.0,1.0);
	}
	
	COLOR = colour;
}