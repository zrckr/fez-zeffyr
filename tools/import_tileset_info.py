import sys
import xml.etree.ElementTree as etree
from pathlib import Path
from dataclasses import dataclass

SCN_HEADER = '[gd_scene load_steps=%d format=2]'
SCN_MESH = '[ext_resource path="res://assets/Trilesets/%s/%s.mesh" type="ArrayMesh" id=%d]'

SCN_START = '''[ext_resource path="res://src/Components/Trile.cs" type="Script" id=1]
[ext_resource path="res://assets/Trilesets/Dev/top_only.png" type="Texture" id=2]
[ext_resource path="res://assets/Trilesets/Dev/all_sides.png" type="Texture" id=3]
[ext_resource path="res://assets/Trilesets/Dev/background.png" type="Texture" id=4]
[ext_resource path="res://assets/Trilesets/Dev/none.png" type="Texture" id=5]'''

SUB_MAT = '''[sub_resource type="SpatialMaterial" id=%d]
flags_transparent = true
albedo_color = Color( 1, 1, 1, 0.90 )
albedo_texture = ExtResource( %d )
uv1_scale = Vector3( 3, 2, 1 )
'''

SUB_MESH = '''[sub_resource type="CubeMesh" id=%d]
material = SubResource( %d )
size = Vector3( %s )
'''

NODE_ROOT = '''[node name="%s" type="Spatial"]'''

NODE_TRILE = '''
[node name="%s" type="MeshInstance" parent="."]
transform = Transform( 1, 0, 0, 0, 1, 0, 0, 0, 1, %s )
mesh = %s
material/0 = null
script = ExtResource( 1 )
BackFace = %d
FrontFace = %d
LeftFace = %d
RightFace = %d
CollisionMask = 24
SurfaceType = %d
Size = Vector3( %s )
CollisionOnly = %s'''

SUF_TYPES = ["None", "Grass", "Metal", "Stone", "Wood"]


@dataclass
class Trile:
    id: int
    name: str
    immaterial: bool
    surface: str
    actor: str
    size: tuple
    faces: list
    geometry: bool


def read_xml(path):
    print('[XML]', path)

    triles = []
    tree = etree.parse(path)
    root = tree.getroot()

    set_name = root.attrib['name'].lower()

    for entry in root.iter('TrileEntry'):
        trile = entry.find('Trile')

        id = entry.attrib['key']
        name = trile.attrib['name']
        surface = trile.attrib['surfaceType']
        immaterial = eval(trile.attrib['immaterial'])

        vec3 = trile.find("Size/Vector3")
        size = (float(vec3.attrib['x']), float(
            vec3.attrib['y']), float(vec3.attrib['z']))

        actor = trile.find("ActorSettings").attrib['type']
        geometry = len(trile.findall(
            "Geometry/ShaderInstancedIndexedPrimitives/Vertices/VertexPositionNormalTextureInstance")) > 0

        faces = [
            face.find("CollisionType").text for face in trile.findall("Faces/Face")]

        data = Trile(id, name, immaterial, surface, actor, size, faces, geometry)
        triles += [data]

    return set_name, triles


def vec2str(vec):
    return ', '.join([f'{v:.02f}' for v in vec])


def most_common(lst):
    return max(set(lst), key=lst.count)


def find_layer(value):
    if value == 'Immaterial':
        return 0
    elif value == 'AllSides':
        return 1
    elif value in ['TopOnly', 'TopNoStraightLedge']:
        return 2
    elif value == 'None':
        return 4
    elif value == 'Ladder':
        return 32
    elif value == 'Vine':
        return 64
    elif value == 'Bouncer':
        return 128
    else:
        return 16


def select_texture(collision):
    if collision == 'None':
        return 4
    elif collision in ['TopOnly', 'TopNoStraightLedge']:
        return 2
    elif collision == 'AllSides':
        return 3
    elif collision == 'Immaterial':
        return 5


path = sys.argv[1]
xml_path = Path(path).resolve()
tscn_path = xml_path.with_suffix(".tscn")

name, triles = read_xml(xml_path)
name = name.capitalize()

ext_list = []
sub_list = []
node_list = []

ext_count = 5
sub_count = 0
node_offset = [0, 0, 0]

node_list += [NODE_ROOT % name]
ext_list += [SCN_START]

trile: Trile
for i, trile in enumerate(triles):
    print('[Trile]', i, '->', trile.name, end=" ")

    _size = vec2str(trile.size)
    _transform = vec2str(node_offset)
    _layers = [find_layer(f) for f in trile.faces]
    _most = most_common(trile.faces)
    _surface = SUF_TYPES.index(trile.surface)

    if trile.geometry:
        ext_count += 1
        ext_list += [SCN_MESH % (name, trile.name, ext_count)]

    if not trile.geometry:
        sub_count += 1
        txt = select_texture(_most)
        sub_list += [SUB_MAT % (sub_count, txt)]

        sub_count += 1
        sub_list += [SUB_MESH % (sub_count, sub_count - 1, _size)]

    if trile.actor != 'None':
        actor = find_layer(trile.actor)
        for i in range(4):
            _layers[i] += actor

    if (trile.immaterial):
        for i in range(4):
            _layers[i] &= 0xFFFF8   # Nuke TopOnly, AllSides, Background

    layer = 0
    for i in range(4):
        layer |= _layers[i]
    print(f"({layer:010b})")

    _mesh = f"SubResource( {sub_count} )" if not trile.geometry else f"ExtResource( {ext_count} )"
    _col = str(not trile.geometry).lower()

    node = NODE_TRILE % (
        trile.name,
        _transform,
        _mesh,
        _layers[0],
        _layers[1],
        _layers[2],
        _layers[3],
        _surface,
        _size,
        _col
    )

    node_list += [node]
    node_offset[0] += 2.0
    if (node_offset[0] > 18.0):
        node_offset[0] = 0.0
        node_offset[2] -= 2.0

header = SCN_HEADER % (ext_count + sub_count)
with open(tscn_path, mode='wt', encoding='utf-8') as tscn:
    print('[TSCN]', tscn_path)
    
    exts = '\n'.join(ext_list)
    subs = '\n'.join(sub_list)
    nodes = '\n'.join(node_list)

    print(header, file=tscn)
    print(file=tscn)
    print(exts, file=tscn)
    print(file=tscn)
    print(subs, file=tscn)
    print(nodes, file=tscn)
