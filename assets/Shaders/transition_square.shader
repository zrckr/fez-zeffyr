shader_type canvas_item;

uniform vec4 from_color : hint_color = vec4(0, 0, 0, 1);
uniform float progress : hint_range(0, 1);
uniform vec2 center = vec2(0.5, 0.5);

float inSquare (vec2 uv, float delta)
{
    delta = 1.0 - delta;
    if (delta == 0.0) return 0.0;
    vec2 o = (uv - center) / delta;
  
    float a = max(abs(o.x), abs(o.y));
    return step(a, delta);
}

void fragment()
{
    vec4 to_color = vec4(0, 0, 0, 0);
    COLOR = mix(from_color, to_color, inSquare(UV, progress));
}