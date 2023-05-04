shader_type canvas_item;

uniform vec4 from_color : hint_color = vec4(0);
uniform vec4 to_color : hint_color = vec4(0, 0, 0, 1);
uniform float progress : hint_range(0, 1);

void fragment()
{
    COLOR = mix(from_color, to_color, progress);
}