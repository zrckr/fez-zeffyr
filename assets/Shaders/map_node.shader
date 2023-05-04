shader_type spatial;
render_mode cull_back;

uniform sampler2D map_screen : hint_albedo;
uniform float scale;

varying float z_dist;

void fragment()
{
    vec2 uv = vec2(SCREEN_UV.x, 1.0 - SCREEN_UV.y / 2f) * scale;
    vec4 map_tex = texture(map_screen, uv);
    
    ALBEDO = map_tex.rgb;
    ALPHA = map_tex.a;
}