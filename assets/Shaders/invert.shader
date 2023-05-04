shader_type canvas_item;

void fragment()
{
    COLOR.rgb = vec3(1.0) - texture(TEXTURE, UV).rgb;
    COLOR.a = texture(TEXTURE, UV).a;
}