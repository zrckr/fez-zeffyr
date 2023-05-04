import bpy
import sys
import xml.etree.ElementTree as etree
from dataclasses import dataclass
from pathlib import Path


@dataclass
class Trile:
    name: str
    vertices: list
    texcoords: list
    indices: list


def rotate_mesh(v):
    import math
    import mathutils

    eul = mathutils.Euler((math.radians(90.0), 0.0, 0.0), 'XYZ')
    vec = mathutils.Vector(v)
    vec.rotate(eul)
    return vec[:]


def parse_xml(xml_path):
    triles = []

    with open(xml_path, 'rt') as file:
        root = etree.fromstring(file.read())
        set_name = root.attrib['name'].lower()

        for entry in root.iter('TrileEntry'):
            name, uvs, verts, idx = "", [], [], []

            for trile in entry.iter("Trile"):
                name = trile.attrib["name"]

                for vertex in trile.findall("Geometry/ShaderInstancedIndexedPrimitives/Vertices/VertexPositionNormalTextureInstance"):
                    pos = vertex.find("Position/Vector3")
                    uv = vertex.find("TextureCoord/Vector2")

                    og_pos = (float(pos.attrib["x"]), float(
                        pos.attrib["y"]), float(pos.attrib["z"]))
                    verts += [rotate_mesh(og_pos)]

                    uvs += [(float(uv.attrib["x"]), float(uv.attrib["y"]))]

                indices = trile.findall(
                    "Geometry/ShaderInstancedIndexedPrimitives/Indices/Index")
                for i in range(0, len(indices), 3):
                    face = (int(indices[i].text), int(
                        indices[i+1].text), int(indices[i+2].text))
                    idx += [face]

            if not (idx or verts):
                continue
            else:
                idx.reverse()
                print(
                    f"[Reading] {name} - verts:{len(verts)}, faces:{len(idx)}, uv:{len(uvs)}")

            triles += [Trile(name, verts, uvs, idx)]

    print("*" * 40)
    return set_name, triles


def create_material(mat_name: str, path: Path):
    print(
        f'[Material] Creating Principled BSDF material `{mat_name}` from {path}')
    mat = bpy.data.materials.new(mat_name)
    mat.use_nodes = True

    nodes = mat.node_tree.nodes
    links = mat.node_tree.links

    tex = nodes.new('ShaderNodeTexImage')
    tex.image = bpy.data.images.load(str(path))
    tex.image.colorspace_settings.name = "sRGB"
    tex.interpolation = 'Closest'

    #out = nodes['Material Output']
    shader = nodes["Principled BSDF"]
    links.new(tex.outputs["Color"], shader.inputs['Base Color'])
    #links.new(shader.outputs['Background'], out.inputs['Surface'])


def convert_to_blend(trile: Trile, mat_name: str, path: Path):
    trile: Trile
    name = trile.name.lower().replace(' ', '_')
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(trile.vertices, [], trile.indices)

    uv_layer = mesh.uv_layers.new()
    mesh.uv_layers.active = uv_layer

    for face in mesh.polygons:
        for vi, li in zip(face.vertices, face.loop_indices):
            uv_layer.data[li].uv[0] = trile.texcoords[vi][0]
            uv_layer.data[li].uv[1] = 1.0 - trile.texcoords[vi][1]

    mesh.update()
    mesh.validate()

    obj = bpy.data.objects.new(trile.name, mesh)
    mat = bpy.data.materials.get(mat_name)

    if obj.data.materials:
        obj.data.materials[0] = mat
    else:
        obj.data.materials.append(mat)

    scene = bpy.context.scene
    scene.collection.objects.link(obj)

    blend_path = path / f"{name}.blend"
    gltf_path = path / f"{name}.gltf"

    # Save as blend
    bpy.ops.wm.save_as_mainfile(filepath=str(blend_path))

    # Save as GLTF
    bpy.ops.export_scene.gltf(
           export_format='GLTF_SEPARATE',
           filepath=str(gltf_path))


if __name__ == '__main__':
    argv = sys.argv
    argv = argv[argv.index("--") + 1:]

    xml_path = Path(argv[0]).resolve()
    png_path = xml_path.with_suffix('.png')

    name, triles = parse_xml(xml_path)
    trile: Trile

    triles_path = xml_path.parent / name
    triles_path.mkdir(parents=True, exist_ok=True)

    for i, trile in enumerate(triles):
        bpy.ops.wm.read_homefile(use_empty=True)
        create_material(name, png_path)

        print(f"[Processing {i+1}/{len(triles)}] -> {trile.name}")
        convert_to_blend(trile, name, triles_path)
        print()