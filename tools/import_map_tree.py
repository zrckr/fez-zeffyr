import os, sys, json
import xml.etree.ElementTree as etree

SIZES = {
    "Lesser": 1.0,
    "Node": 2.0,
    "Hub": 3.0
}

OFFSETS = {
    "Left": "-%d, 0, 0",
    "Down": "0, -%d, 0",
    "Back": "0, 0, -%d",
    "Right": "%d, 0, 0",
    "Top": "0, %d, 0",
    "Front": "0, 0, %d",
}

UP_LEVELS = [
    "TREE_ROOTS",
    "TREE",
    "TREE_SKY",
    "FOX",
    "WATER_TOWER",
    "PIVOT_WATERTOWER",
    "VILLAGEVILLE_3D"
]

DOWN_LEVELS = [
    "SEWER_START",
    "MEMORY_CORE",
    "ZU_FORK",
    "STARGATE",
    "QUANTUM"
]

OP_LEVELS = [
    "NUZU_SCHOOL",
    "NUZU_ABANDONED_A",
    "ZU_HOUSE_EMPTY_B",
    "PURPLE_LODGE",
    "ZU_HOUSE_SCAFFOLDING",
    "MINE_BOMB_PILLAR",
    "CMY_B",
    "INDUSTRIAL_HUB",
    "SUPERSPIN_CAVE",
    "GRAVE_LESSER_GATE",
    "THRONE",
    "VISITOR",
    "ORRERY",
    "LAVA_SKULL",
    "LAVA_FORK"
]

BACK_LEVELS = [
    "ABANDONED_B",
    "LAVA"
]

FRONT_LEVELS = [
    "VILLAGEVILLE_3D",
    "ZU_LIBRARY"
]

RIGHT_LEVELS = [
    "ZU_ZUISH",
    "ZU_UNFOLD",
    "BELL_TOWER",
    "CLOCK",
    "ZU_TETRIS"
]


MAP_NODE = '''
[node name="%s" parent="%s" instance=ExtResource( 1 )]
transform = Transform( %s )
_conditions = {
"Big Cubes": %d,
"Chests": %d,
"Locked Doors": %d,
"Other": %d,
"Secrets": %d,
"Small Cubes": %d,
"Unlocked Doors": %d
}
UnknownColor = Color( 0.5, 0.5, 0.5, 0.5 )
State = %d
NodeSize = %d
HasWarpGate = %s
MapScreen = ExtResource( %d )
HasLesserGate = %s
'''

GD_HEAD = '''[gd_scene load_steps=150 format=2]

[ext_resource path="res://src/Components/Map/MapNode.tscn" type="PackedScene" id=1]
%s
'''

GD_TEXTURE = '[ext_resource path="res://assets/Other Textures/map_screens/%s.png" type="Texture" id=%d]'
GD_ROOT = '\n[node name="MapTree" type="Spatial"]\n'

CONN_COUNTER = 0

def parse_tree(node_xml):
    global CONN_COUNTER

    node = {
        "name": node_xml.attrib['name'],
        "lesser": node_xml.attrib['hasLesserGate'].lower(),
        "warp": node_xml.attrib['hasWarpGate'].lower(),
        "size": SIZES[node_xml.attrib['type']],
    }

    win = node_xml.find('WinConditions')
    node['_wins'] = {
        "chests":   int(win.attrib['chests']),
        "locked":   int(win.attrib['lockedDoors']),
        "unlocked": int(win.attrib['unlockedDoors']),
        "big":      int(win.attrib['cubeShards']),
        "small":    int(win.attrib['splitUp']),
        "secrets":  int(win.attrib['secrets']),
        "others":   int(win.attrib['others']),
    }

    scripts = win.findall('Scripts/Script')
    if (len(scripts)):
        words = [s.text for s in scripts]
        strs = ', '.join('"{0}"'.format(w) for w in words)
        node['_wins']['scripts'] = f'PoolStringArray( {strs} )'
    else:
        node['_wins']['scripts'] = 'null'

    node['_conns'] = []
    conns = node_xml.findall("Connections/Connection")
    
    if (len(conns)):
        for i, conn in enumerate(conns):
            next_node = conn.find('Node')
        
            node['_conns'] += [{
                'id': CONN_COUNTER,
                'face': conn.attrib['face'],
                'branch': float(conn.attrib['branchOversize']),
            }]
            CONN_COUNTER += 1
            node['_conns'][i]['child'] = parse_tree(next_node)
   
    return node

def read_xml(path):
    with open(path, 'rt') as file:
        print(f'[Info] Reading {path}...', end='')
        root = etree.fromstring(file.read())
    
    tree = etree.ElementTree(root)
    result = parse_tree(root.find('Node'))
    
    print('Done!')
    return result

GD_NODE = []
GD_TEX = []
GD_EXT_COUNT = 1

def generate_scn2(node: dict, path: str, offset: str):
    global GD_NODE, GD_TEX, GD_EXT_COUNT

    print("[Generate]", f"{node['name']}: {path.lower()}")
    GD_EXT_COUNT += 1

    transform = "1, 0, 0, 0, 1, 0, 0, 0, 1, " + offset
    
    # Add MapNode
    GD_NODE += [MAP_NODE % (
        # Node information
        node['name'],
        path,
        transform,
        
        # Winning conditions
        node['_wins']['big'],
        node['_wins']['chests'],
        node['_wins']['locked'],
        node['_wins']['others'],
        node['_wins']['secrets'],
        node['_wins']['small'],
        node['_wins']['unlocked'],
        
        # Node params
        1,              # Discovered state
        node['size'],
        node['warp'],
        GD_EXT_COUNT,   # Current MapScreen texture
        node['lesser'],
    )]

    # Add MapScreen texture to the header
    GD_TEX += [GD_TEXTURE % (
        node['name'].lower(), GD_EXT_COUNT
    )]

    if (len(node["_conns"])):
        for conn in node["_conns"]:
            next_path = path.replace('.', '')
            next_path = os.path.join(next_path, node['name']).replace("\\", "/")
            
            next_offset = OFFSETS[conn['face']] % (node['size'] * 4)
            
            generate_scn2(conn['child'], next_path, next_offset)

def convert_to_tscn(path, data):
    generate_scn2(data, ".", "0, 0, 0")
    
    print(f'''[TSCN] Writing to {path}...''', end=" ")
    with open(path, 'wt') as tscn:
        tscn.write(GD_HEAD % ('\n'.join(GD_TEX)))
        tscn.write(GD_ROOT)

        for i in range(len(GD_NODE)):
            tscn.write(GD_NODE[i])
       
    print(f'''Done!''')
    return

if __name__ == "__main__":
    abs_path = os.path.dirname(__file__)
    xml_path = os.path.join(abs_path, "maptree.xml")
    tscn_path = os.path.join(abs_path, "maptree2.tscn")
    json_path = os.path.join(abs_path, "maptree.json")

    data = read_xml(xml_path)
    with open(json_path, 'wt') as json_file:
        print("[Saving]", "Temp results in maptree.json...")
        json_file.write(json.dumps(data, indent=4))

    convert_to_tscn(tscn_path, data)