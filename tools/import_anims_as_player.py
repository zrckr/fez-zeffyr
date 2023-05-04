import os, sys, glob
import wordninja
from pathlib import Path
import xml.etree.ElementTree as etree

ANIM_SUB_RSCR = """
[sub_resource type="Animation" id=%d]
resource_name = "%s"
length = %s
tracks/0/type = "value"
tracks/0/path = NodePath("AnimationPlayer/Sprite:texture")
tracks/0/interp = 0
tracks/0/loop_wrap = true
tracks/0/imported = false
tracks/0/enabled = true
tracks/0/keys = {
"times": PoolRealArray( 0 ),
"transitions": PoolRealArray( 1 ),
"update": 0,
"values": [ ExtResource( %d ) ]
}
tracks/1/type = "value"
tracks/1/path = NodePath("AnimationPlayer/Sprite:region_rect")
tracks/1/interp = 0
tracks/1/loop_wrap = true
tracks/1/imported = false
tracks/1/enabled = true
tracks/1/keys = {
"times": PoolRealArray( %s ),
"transitions": PoolRealArray( %s ),
"update": 0,
"values": [ %s ]
}
"""

ANIM_PLAYER = """anims/%s = SubResource( %s )
"""

ANIM_EXT = """[ext_resource path="res://Assets/Character Animations/Gomez/%s.ani.png" type="Texture" id=%d]
"""

def load_xml(path):
    with open(path, "rt") as xml:
        print(f'[Info] Reading {path}')
        root = etree.fromstring(xml.read())

    size = ( int(root.attrib['width']), int(root.attrib['height']) )
    actual = ( int(root.attrib['actualWidth']), int(root.attrib['actualHeight']) )
    
    frames, durations = [], []
    for frame in root.findall('Frames/FramePC'):
        durations += [int(frame.attrib['duration']) / 10_000 / 1_000]
        rect = frame.find('Rectangle')
        frames += [ f"Rect2 ({rect.attrib['x']}, {rect.attrib['y']}, {rect.attrib['w']}, {rect.attrib['h']})" ]

    return {
        'size': size,
        'actual': actual,
        'frames': frames,
        'durations': durations,
    }

def time_sum(durations):
    _sum, _times = 0.0, []
    for d in durations:
        _times += [round(_sum, 2)]
        _sum += d
    return _times, f'{sum(durations):.3}'

ext_offset = 1
offset = 3
xmls = glob.glob(sys.argv[1] + '\*.xml')
#xmls = glob.glob('Z:\zeffyr\Private\sprites\gomez\idleplay.xml')

ani_internal_exts = '\n[node name="AnimationPlayer" type="AnimationPlayer" parent="."]\n'
txt_subs = ""
txt_exts = ""

for i, xml in enumerate(xmls):
    name = Path(xml).stem
    data = load_xml(xml)
    dlen = len(data['frames'])

    new_name = "_".join(wordninja.split(name))
    raw_durs, length = time_sum(data['durations'])
    
    times = ', '.join(map(str, raw_durs))
    transitions = ', '.join(["1" for i in range(dlen)])
    values = ', '.join(data['frames'])
    
    # times = ", ".join([f"{x * time_step:.2f}" for x in range(len(data['frames']))])
    # trans = ", ".join(["1" for x in range(len(data['frames']))])
    # vals = ", ".join([str(x) for x in range(len(data['frames']))])

    txt_subs += ANIM_SUB_RSCR % (i+offset, new_name, length, i+ext_offset, times, transitions, values)
    txt_exts += ANIM_EXT % (name, i+ext_offset)
    
    ani_internal_exts += ANIM_PLAYER % (new_name, i+offset)
    
abs_path = os.path.dirname(__file__)
txt_path = abs_path + '\\' + 'gomez_player_anis.txt'
with open(txt_path, 'wt') as txt:
    txt.write(txt_exts)
    txt.write(txt_subs)
    txt.write(ani_internal_exts)