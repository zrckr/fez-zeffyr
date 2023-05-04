shader_type spatial;
render_mode unshaded, cull_disabled;

uniform sampler2D anim_texture : hint_albedo;
uniform bool silhouette;
uniform float background;

uniform bool color_swap;
uniform bool no_more_fez;
uniform vec4 red_swap : hint_color;
uniform vec4 black_swap : hint_color;
uniform vec4 white_swap : hint_color;
uniform vec4 yellow_swap : hint_color;
uniform vec4 gray_swap : hint_color;

void fragment() {
	vec4 albedo = texture(anim_texture, UV);
    
    if (no_more_fez)
    {
        vec3 red = abs(albedo.rgb - red_swap.rgb);
        vec3 yellow = abs(albedo.rgb - yellow_swap.rgb);
        
        if (length(red) < 0.5 || length(yellow) < 0.1)
        {
            albedo.a = 0f;
        }
    }
    
    ALBEDO = albedo.rgb;
    ALPHA = albedo.a;
    
    if (silhouette)
    {
    }
}
