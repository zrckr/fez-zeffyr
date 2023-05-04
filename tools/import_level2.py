import sys
import xml.etree.ElementTree as etree
from typing import Dict, Tuple, List
from pathlib import Path
from dataclasses import dataclass
from struct import pack
from godot_parser import GDScene, Node, GDObject, NodePath, ExtResource
from PIL import ImageColor


@dataclass(order=True)
class Vec3:
    x: float = 0.0
    y: float = 0.0
    z: float = 0.0

    def from_xml(xml):
        x = float(xml.attrib['x'])
        y = float(xml.attrib['y'])
        z = float(xml.attrib['z'])
        return Vec3(x, y, z)

    def to_obj(self) -> GDObject:
        return GDObject("Vector3", self.x, self.y, self.z)


@dataclass(order=True)
class Quat:
    x: float = 0.0
    y: float = 0.0
    z: float = 0.0
    w: float = 0.0

    def from_xml(xml):
        x = float(xml.attrib['x'])
        y = float(xml.attrib['y'])
        z = float(xml.attrib['z'])
        w = float(xml.attrib['w'])
        return Quat(x, y, z, w)

    def to_obj(self) -> GDObject:
        return GDObject("Quat", self.x, self.y, self.z, self.w)


@dataclass(order=True)
class Transform:
    matrix: List[int]

    def form(pos: Vec3 = Vec3(0, 0, 0), rot: Quat = Quat(0, 0, 0, 1), scl: Vec3 = Vec3(1, 1, 1)):
        matrix = list(range(12))

        xx = rot.x * rot.x
        xy = rot.x * rot.y
        xz = rot.x * rot.z
        yy = rot.y * rot.y
        zz = rot.z * rot.z
        yz = rot.y * rot.z

        wx = rot.w * rot.x
        wy = rot.w * rot.y
        wz = rot.w * rot.z

        matrix[0] = scl.x * (1.0 - 2.0*(yy + zz))
        matrix[1] = scl.x * (2.0*(xy - wz))
        matrix[2] = scl.x * (2.0*(xz + wy))

        matrix[3] = scl.y * (2.0*(xy + wz))
        matrix[4] = scl.y * (1.0 - 2.0*(xx + zz))
        matrix[5] = scl.y * (2.0*(yz - wx))

        matrix[6] = scl.z * (2.0*(xz - wy))
        matrix[7] = scl.z * (2.0*(yz + wx))
        matrix[8] = scl.z * (1.0 - 2.0*(xx + yy))

        matrix[9] = pos.x
        matrix[10] = pos.y
        matrix[11] = pos.z

        return Transform(matrix)

    def ones():
        matrix = [0 for i in range(12)]
        matrix[0], matrix[4], matrix[8] = 1, 1, 1
        return Transform(matrix)

    def to_obj(self) -> GDObject:
        rounded = [round(m, 4) for m in self.matrix]
        return GDObject("Transform", *rounded)


class Volume:
    def __init__(self, xml) -> None:
        volume = xml.find('Volume')

        self.key = xml.attrib['key']
        self.start = Vec3.from_xml(volume.find('From/Vector3'))
        self.end = Vec3.from_xml(volume.find('To/Vector3'))

        self.faces = []
        for face in volume.findall('Orientations/FaceOrientation'):
            self.faces += [face.text]
        pass

    @property
    def area(self) -> Tuple[Vec3, Vec3]:
        size = Vec3(
            abs(self.start.x - self.end.x) / 2.0,
            abs(self.start.y - self.end.y) / 2.0,
            abs(self.start.z - self.end.z) / 2.0
        )
        pos = Vec3(
            (self.start.x + self.end.x) / 2.0,
            (self.start.y + self.end.y) / 2.0,
            (self.start.z + self.end.z) / 2.0
        )
        return (size, pos)


class Trile:
    def __init__(self, xml, trileset: list) -> None:
        self.id1 = xml.attrib['trileId']
        self.id2 = int(trileset[0].index(self.id1))
        self.name = trileset[1][self.id2]
        self.rot = int(xml.attrib['orientation'])
        self.pos = Vec3.from_xml(xml.find('Position/Vector3'))
        pass


class Art:
    def __init__(self, xml) -> None:
        self.key = xml.attrib['key']
        self.name = xml.find('ArtObjectInstance').attrib['name'].lower()
        self.pos = Vec3.from_xml(
            xml.find('ArtObjectInstance/Position/Vector3'))
        self.scale = Vec3.from_xml(xml.find('ArtObjectInstance/Scale/Vector3'))
        self.rot = Quat.from_xml(
            xml.find('ArtObjectInstance/Rotation/Quaternion'))
        pass


class Plane:
    def __init__(self, xml) -> None:
        self.key = xml.attrib['key']
        pl = xml.find("BackgroundPlane")

        self.pos = Vec3.from_xml(pl.find('Position/Vector3'))
        self.scale = Vec3.from_xml(pl.find('Scale/Vector3'))
        self.rot = Quat.from_xml(pl.find('Rotation/Quaternion'))

        self.repeat = (
            eval(pl.attrib['xTextureRepeat']),
            eval(pl.attrib['yTextureRepeat'])
        )
        self.animated = eval(pl.attrib['animated'])
        self.name = pl.attrib['textureName'].lower()
        self.dsided = eval(pl.attrib['doubleSided'])
        self.opacity = float(pl.attrib['opacity'])
        self.billboard = eval(pl.attrib['billboard'])
        self.filter = pl.attrib['filter']
        self.actor = pl.attrib['actorType']
        pass


class Group:
    def __init__(self, xml, trileset) -> None:
        self.key = xml.attrib['key']
        self.actor = xml.find('TrileGroup').attrib['actorType']

        self.triles = []
        for trile in xml.findall('TrileGroup/Triles/TrileInstance'):
            self.triles += [Trile(trile, trileset)]
        pass


class Npc:
    def __init__(self, xml) -> None:
        instance = xml.find("NpcInstance")

        self.key = xml.attrib['key']
        self.pos = Vec3.from_xml(instance.find('Position/Vector3'))
        self.dest = Vec3.from_xml(instance.find('DestinationOffset/Vector3'))

        self.name = instance.attrib['name']
        self.walk = float(instance.attrib['walkSpeed'])
        self.random = eval(instance.attrib['randomizeSpeech'])
        self.once = eval(instance.attrib['sayFirstSpeechLineOnce'])
        self.avoid = eval(instance.attrib['avoidsGomez'])
        self.actor = instance.attrib['actorType']

        self.lines = []
        for line in instance.findall('Speech/SpeechLine'):
            self.lines += [line.attrib['text']]

        self.actions = []
        for action in instance.findall('Actions/Action'):
            anim = action.find("NpcActionContent").attrib['animationName']
            self.actions += [anim]
        pass


class Track:
    def __init__(self, xml) -> None:
        self.cycle = (
            xml.attrib['dawn'],
            xml.attrib['day'],
            xml.attrib['dusk'],
            xml.attrib['night'])
        self.name = xml.attrib['name']
        pass


class Attributes:
    def __init__(self, xml) -> None:
        self.name = xml.attrib['name']
        self.trileset = xml.attrib['trileSetName'].lower()

        self.start = Vec3.from_xml(
            xml.find('StartingPosition/TrileFace/TrileId/TrileEmplacement'))
        self.size = Vec3.from_xml(xml.find('Size/Vector3'))

        if 'songName' in xml.attrib:
            self.song = xml.attrib['songName']
        self.node = xml.attrib['nodeType']
        self.rainy = xml.attrib['rainy']
        self.halo = xml.attrib['haloFiltering']
        self.water_type = xml.attrib['waterType']

        self.ambient = xml.attrib['baseAmbient']
        self.diffuse = xml.attrib['baseDiffuse']
        self.water_height = xml.attrib['waterHeight']
        pass


class Level:
    def __init__(self, path, path2) -> None:
        tree = etree.parse(path)
        root = tree.getroot()

        self.trile_path = path2
        self.attribs = Attributes(root)

        trileset = self.read_trileset(self.attribs.trileset)

        self.volumes = []
        for volume in root.findall('Volumes/Entry'):
            self.volumes += [Volume(volume)]

        self.triles = []
        for trile in root.findall('Triles/Entry/TrileInstance'):
            self.triles += [Trile(trile, trileset)]

        self.arts = []
        for art in root.findall('ArtObjects/Entry'):
            self.arts += [Art(art)]

        self.planes = []
        for plane in root.findall('BackgroundPlanes/Entry'):
            self.planes += [Plane(plane)]

        self.groups = []
        for group in root.findall('Groups/Entry'):
            self.groups += [Group(group, trileset)]

        self.npcs = []
        for npc in root.findall('NonplayerCharacters/Entry'):
            self.npcs += [Npc(npc)]

        self.tracks = []
        for track in root.findall('AmbienceTracks/AmbienceTrack'):
            self.tracks += [Track(track)]
        pass

    def read_trileset(self, name):
        trile_path = Path(self.trile_path, name).with_suffix('.xml')
        tree = etree.parse(trile_path)
        root = tree.getroot()

        keys = []
        names = []
        for entry in root.iter('TrileEntry'):
            keys += [entry.attrib['key']]
            names += [entry.find('Trile').attrib['name']]

        return [keys, names]


ROT_INDICES = [10, 22, 0, 16]
FACE_INDICES = {
    'Back': Quat(0, 1, 0, 0),
    'Left': Quat(0, -0.7071068, 0, 0.7071068),
    'Front': Quat(0, 0, 0, 1),
    'Right': Quat(0, 0.7071068, 0, 0.7071068)
}


class GodotScene:
    level: Level
    path: Path
    scene: GDScene

    def __init__(self, level: Path, triles: Path) -> None:
        self.level = Level(level, triles)
        self.path = level.with_suffix(".tscn")
        self.scene = GDScene()
        pass

    def make_scene(self) -> None:
        node: Node
        parent: Node

        gomez_rsrc = self.scene.add_ext_resource(
            "res://scenes/Gomez.tscn", "PackedScene")
        camera_rsrc = self.scene.add_ext_resource(
            "res://scenes/GameCamera.tscn", "PackedScene")

        gomez = Node("Gomez", instance=gomez_rsrc.reference.id, properties={
            "transform": Transform.form(self.level.attribs.start).to_obj()
        })
        camera = Node("GameCamera", instance=camera_rsrc.reference.id, properties={
            "PixelsPerTrixel": 2,
            "TargetPath": NodePath("../Gomez")
        })

        water = None
        if (self.level.attribs.water_type != 'None'):
            water_rsrc = self.scene.add_ext_resource(
                "res://scenes/Water.tscn", "PackedScene")
            water = Node("Water", instance=water_rsrc.reference, properties={
                "Height": self.level.attribs.water_height,
                "Type": self.level.attribs.water_type,
            })

        meshlib = self.scene.add_ext_resource(
            f"res://assets/Trilesets/{self.level.attribs.trileset}.meshlib", "MeshLibrary")

        triles = self.make_triles(meshlib.reference)
        groups = self.make_groups(meshlib.reference)
        arts = self.make_arts()
        planes = self.make_planes()
        volumes = self.make_volumes()
        npcs = self.make_npcs()

        with self.scene.use_tree() as tree:
            tree.root = Node(self.level.attribs.name, "Spatial")
            tree.root.add_child(triles)
            if (water):
                tree.root.add_child(water)
            tree.root.add_child(gomez)
            tree.root.add_child(camera)

            parent = Node("Groups", "Spatial")
            for node in groups:
                parent.add_child(node)
            tree.root.add_child(parent)

            parent = Node("Arts", "Spatial")
            for node in arts:
                parent.add_child(node)
            tree.root.add_child(parent)

            parent = Node("Planes", "Spatial")
            for node in planes:
                parent.add_child(node)
            tree.root.add_child(parent)

            parent = Node("Volumes", "Spatial")
            for node in volumes:
                parent.add_child(node)
            tree.root.add_child(parent)

            parent = Node("Npcs", "Spatial")
            for node in npcs:
                parent.add_child(node)
            tree.root.add_child(parent)

        self.scene.write(self.path)
        pass

    def make_triles(self, ext: ExtResource) -> Node:
        return self.generate_gridmap('Triles', self.level.triles, ext)

    def make_groups(self, ext: ExtResource) -> List[Node]:
        group: Group
        groups: List[Node] = []

        for group in self.level.groups:
            gridmap = self.generate_gridmap(group.key, group.triles, ext)
            groups += [gridmap]
        return groups

    def make_arts(self) -> List[Node]:
        art: Art
        arts: List[Node] = []
        dict1: Dict = {}

        names = {art.name for art in self.level.arts}
        for name in names:
            dict1[name] = self.scene.add_ext_resource(
                f"res://assets/Art Objects/{name}.gltf", "PackedScene")

        for art in self.level.arts:
            transform = Transform.form(art.pos, art.rot, art.scale)

            instance = Node(art.key, instance=dict1[art.name].reference.id, properties={
                "transform": transform.to_obj()
            })
            arts += [instance]
        return arts

    def make_planes(self) -> List[Node]:
        plane: Plane
        planes: List[Plane] = []
        dict1: Dict = {}
        dict2: Dict = {}

        names = {(plane.name, plane.animated) for plane in self.level.planes}
        for name, animated in names:
            if (animated):
                dict1[name] = self.scene.add_ext_resource(
                    f"res://assets/Background Planes/{name}.tres", "SpriteFrames")
            else:
                dict2[name] = self.scene.add_ext_resource(
                    f"res://assets/Background Planes/{name}.png", "Texture")

        for plane in self.level.planes:
            transform = Transform.form(plane.pos, plane.rot, plane.scale)
            typeof = 'AnimatedSprite3D' if plane.animated else 'Sprite3D'
            color = self.convert_color8(plane.filter)

            props = {
                "transform": transform.to_obj(),
                "modulate": color,
                "opacity": plane.opacity,
                "pixel_size": 0.0625,
                "billboard": 2 if plane.billboard else 0,
                "transparent": True,
                "shaded": True,
                "double_sided": plane.dsided,
                "alpha_cut": 2,
            }
            if (plane.animated):
                props['frames'] = dict1[plane.name].reference
                props['animation'] = plane.name
                props['playing'] = True
            else:
                props['texture'] = dict2[plane.name].reference

            sprite = Node(plane.name, type=typeof, properties=props)
            planes += [sprite]
        return planes

    def make_volumes(self) -> List[Node]:
        volume: Volume
        volumes: List[Volume] = []

        for volume in self.level.volumes:
            size, pos = volume.area
            rot = FACE_INDICES[volume.faces[0]]
            transform = Transform.form(pos, rot)

            boxshape = self.scene.add_sub_resource('BoxShape')
            boxshape.properties['extents'] = size.to_obj()

            area = Node(volume.key, 'Area', properties={
                "transform": transform.to_obj(),
            })
            area.add_child(Node("Shape", "CollisionShape", properties={
                "shape": boxshape.reference
            }))

            volumes += [area]
        return volumes

    def make_npcs(self) -> List[Node]:
        npc: Npc
        name: str
        npcs: List[Npc] = []
        dict1: Dict = {
            None: self.scene.add_ext_resource(
                f"res://src/Components/NpcInstance.cs", "Script"),
        }

        names = [npc.name for npc in self.level.npcs]
        for name in names:
            name = name.replace('_', ' ').title()
            if ('Mcmayor' in name):
                name = 'Mayor McMayor'
            if (name not in dict1):
                dict1[name] = self.scene.add_ext_resource(
                    f"res://assets/Character Animations/{name}/{name}.tres", "SpriteFrames")

        i = 1
        for npc in self.level.npcs:
            transform = Transform.form(npc.pos)
            name = npc.name.replace('_', ' ').title()
            if ('Mcmayor' in name):
                name = 'Mayor McMayor'

            frames = dict1[name].reference
            if (names.count(name) > 1):
                name += str(i)
                i += 1

            npc_instance = Node(name, "AnimatedSprite3D", properties={
                "transform": transform.to_obj(),
                "pixel_size": 0.0625,
                "frames": frames,
                "animation": npc.actions[0].lower(),
                "script": dict1[None].reference,
                "WalkSpeed": npc.walk,
                "AvoidsGomez": npc.avoid,
                "RandomizeSpeech": npc.random,
                "SayFirstSpeechLineOnce": npc.once,
                "SpeechTags": GDObject('PoolStringArray', *npc.lines),
                "DestinationOffset": npc.dest.to_obj(),
            })
            npcs += [npc_instance]
        return npcs

    def generate_gridmap(self, name: str, triles: List[Trile], ext: ExtResource) -> Node:
        transform = Transform.form(Vec3(0.5, 0.5, 0.5))
        cells: List[str] = []
        trile: Trile

        for trile in triles:
            if ('key' in trile.name.lower() or 'cube' in trile.name.lower()):
                continue

            rot = ROT_INDICES[trile.rot]
            cell = self.generate_gridmap_cell(trile.pos, trile.id2, rot)
            cells += cell

        gridmap = Node(name, 'GridMap', properties={
            "transform": transform.to_obj(),
            "mesh_library": ext,
            "cell_size": Vec3(1, 1, 1).to_obj(),
            "cell_center_x": False,
            "cell_center_y": False,
            "cell_center_z": False,
            "collision_layer": 0,
            "collision_mask": 0,
            "data": {
                "cells": GDObject("PoolIntArray", *cells)
            }
        })
        return gridmap

    def generate_gridmap_cell(self, pos: Vec3, id: int, rot: int):
        # pos -> 64bit (unsigned 16bit for x, y, z) -> index (i1, i2)
        # id, rot ->  32bit (8bit lsb for id, rot) -> cell (i3)
        # 10100000 (?) 00010110 (rot) 00000000 (?) 11010000 (id)

        double = pack('3h', int(pos.x), int(pos.y), int(pos.z))
        single = pack('2H', id, rot)

        i1 = int.from_bytes(double[:4], "little")
        i2 = int.from_bytes(double[4:], "little")
        i3 = int.from_bytes(single, "little")

        return [i1, i2, i3]

    def convert_color8(self, color: str):
        color = ImageColor.getrgb(color)
        values = [float(i) / 255.0 for i in color]
        return GDObject('Color', *values)


if (len(sys.argv) <= 0):
    exit(-1)

path = sys.argv[1]
xml_path = Path(path).resolve()
trile_path = Path("Z:\\zeffyr\\Fez Assets\\trile sets").resolve()
godot = GodotScene(xml_path, trile_path)
godot.make_scene()
