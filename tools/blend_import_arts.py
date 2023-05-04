import os, bpy, sys
import xml.etree.ElementTree as etree

def rotate(v):
    import math
    import mathutils
    
    eul = mathutils.Euler((math.radians(90.0), 0.0, 0.0), 'XYZ')
    vec = mathutils.Vector(v)
    vec.rotate(eul)
    return vec[:]

def parse(xml_path):
    art_dict = {}
    name = ""

    with open(xml_path, 'rt') as file:
        root = etree.fromstring(file.read())
        

        name = root.attrib['name'].lower()
        normals, uvs, verts, idx = [], [], [], []

        size = root.find('Size/Vector3')
        art_size = [( float(size.attrib["x"]), float(size.attrib["y"]), float(size.attrib["z"]) )]
        
        for vertex in root.findall("ShaderInstancedIndexedPrimitives/Vertices/VertexPositionNormalTextureInstance"):
            pos = vertex.find("Position/Vector3")
            uv = vertex.find("TextureCoord/Vector2")
            normal = vertex.find("Normal").text

            og_pos = ( float(pos.attrib["x"]), float(pos.attrib["y"]), float(pos.attrib["z"]) )
            verts += [rotate(og_pos)]

            uvs += [( float(uv.attrib["x"]), float(uv.attrib["y"]) )]

        indices = root.findall("ShaderInstancedIndexedPrimitives/Indices/Index")
        for i in range(0, len(indices), 3):
            face = (int(indices[i].text), int(indices[i+1].text), int(indices[i+2].text)) 
            idx += [face]

        if not (idx or verts or normals):
            raise Exception("Looks like we're dealing with a ghost!")
        else:
            idx.reverse()
            print(f"[Reading] {name} - verts:{len(verts)}, faces:{len(idx)}, uv:{len(uvs)}")
            
            art_dict = {
                "name": name,
                "vertex": verts,
                "uv": uvs,
                "indices": idx
            }   
    return name, art_dict

def create_material(image_path, mat_name):
    print(f'[Material] Creating Principled BSDF material `{mat_name}` from {image_path}')
    mat = bpy.data.materials.new(mat_name)
    mat.use_nodes = True

    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    
    tex = nodes.new('ShaderNodeTexImage')
    tex.image = bpy.data.images.load(image_path)
    tex.image.colorspace_settings.name = "sRGB"
    tex.interpolation = 'Closest'
    
    #out = nodes['Material Output']
    shader = nodes["Principled BSDF"]
    links.new(tex.outputs["Color"], shader.inputs['Base Color'])
    #links.new(shader.outputs['Background'], out.inputs['Surface'])

def convert(art, mat_name):
    name = art['name'].lower().replace(' ', '_')
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(art['vertex'], [], art['indices'])
        
    uv_layer = mesh.uv_layers.new()
    mesh.uv_layers.active = uv_layer
        
    for face in mesh.polygons:
        for vi, li in zip(face.vertices, face.loop_indices):
            uv_layer.data[li].uv[0] = art['uv'][vi][0]
            uv_layer.data[li].uv[1] = 1.0 - art['uv'][vi][1]
        
    mesh.update()
    mesh.validate()
        
    obj = bpy.data.objects.new(art['name'], mesh)
    mat = bpy.data.materials.get(mat_name)
        
    if obj.data.materials:
        obj.data.materials[0] = mat
    else:
        obj.data.materials.append(mat)
        
    obj.location.x = 0
    obj.location.y = 0
        
    scene = bpy.context.scene
    scene.collection.objects.link(obj)
    print(f"[Processing] -> {art['name']}")
        
if __name__ == '__main__': 
    argv = sys.argv
    argv = argv[argv.index("--") + 1:]
    bpy.ops.wm.read_homefile(use_empty=True)
    
    filename = os.path.splitext(argv[0])[0]
    abs_path = os.path.dirname(os.path.abspath(__file__))
    
    xml_path = os.path.join(abs_path, filename+'.xml')
    png_path = os.path.join(abs_path, filename+'.png')
    
    name, data = parse(xml_path)
    create_material(png_path, name)
    convert(data, name)
    
    bpy.ops.wm.save_as_mainfile(filepath=filename+'.blend')
    bpy.ops.export_scene.gltf(export_format='GLTF_EMBEDDED', 
                              export_copyright='Copyright © Zerocker 2020',
                              filepath=filename)