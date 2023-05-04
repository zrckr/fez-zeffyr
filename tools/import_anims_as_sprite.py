import sys
import xml.etree.ElementTree as etree
from pathlib import Path, PurePath

TRES_HEADER = """[gd_resource type="SpriteFrames" load_steps={} format=2]"""
TRES_RESOURCE = """[ext_resource path="res://assets/Character Animations/{}/{}.ani.png" type="Texture" id={}]"""

ATLAS_SUB_RSRC = """[sub_resource type="AtlasTexture" id={}]
flags = 16
atlas = ExtResource( {} )
region = Rect2( {} )
"""

RSRC_ANIMATIONS = """"frames": [ {} ],
"loop": true,
"name": "{}",
"speed": {:.2f}"""

RSCR_ANI_HEADER = """[resource]
animations = [ {"""


def load_xml(path):
    with open(path, "rt") as xml:
        print('[Read]', path)
        root = etree.fromstring(xml.read())

    size = (int(root.attrib['width']), int(root.attrib['height']))
    actual = (int(root.attrib['actualWidth']),
              int(root.attrib['actualHeight']))

    frames, duration = [], 0
    for frame in root.findall('Frames/FramePC'):
        duration += float(frame.attrib['duration']) / 10_000 / 1000
        rect = frame.find('Rectangle')
        frames += [(rect.attrib['x'], rect.attrib['y'],
                    rect.attrib['w'], rect.attrib['h'])]

    return {
        'size': size,
        'actual': actual,
        'frames': frames,
        'duration': duration,
    }


#root = Path("D:\\Zeffyr\\tools\\anims").resolve()
root = Path(sys.argv[1]).resolve()

# Group xmls by NPC
groups = {}
for child in root.glob('**/*'):
    if child.is_dir():
        groups[child.stem] = [path.resolve() for path in child.rglob("*.xml") if "metadata" not in str(path)]

# Generate unique .tres for each animation
for name, xmls in groups.items():
    tres_exts, tres_subs, tres_anis = [], [], [ RSCR_ANI_HEADER ]
    tres_steps = 1
    
    for i, xml in enumerate(xmls):
        data = load_xml(str(xml))
        anis = []

        tres_exts += [ TRES_RESOURCE.format(xml.parent.stem, xml.stem, i+1) ]
        for frame in data['frames']:
            tres_subs += [ ATLAS_SUB_RSRC.format(tres_steps, i+1, ", ".join(frame)) ]
            anis += [ f'SubResource( {tres_steps} )' ]
            tres_steps += 1

        tres_anis += [ RSRC_ANIMATIONS.format(', '.join(anis), xml.stem, 7.0) ]
        
        if (i != len(xmls) - 1):
            tres_anis += ['}, {']
        else:
            tres_anis += ['} ]']

    tres_header = TRES_HEADER.format(tres_steps + len(xmls))
    tres_path = str(PurePath(xml.parent, name)) + ".tres"
    
    tres_exts = "\n".join(tres_exts)
    tres_subs = "\n".join(tres_subs)
    tres_anis = "\n".join(tres_anis)

    print("[Write]", tres_path)
    print("*" * 40)
    
    with open(tres_path, 'wt') as tres:
        print(tres_header, file=tres, end="\n\n")
        print(tres_exts, file=tres, end="\n\n")
        print(tres_subs, file=tres)
        print(tres_anis, file=tres)