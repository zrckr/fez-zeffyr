from sys import argv
from pathlib import Path
from godot_parser import load
import multiprocessing as mp


def edit_tres(path: Path):
    scene = load(path)

    texture = f"res://assets/Art Objects/{path.stem}.png"
    count = len(scene._sections)

    if (count <= 3):
        return
    if (count > 4):
        scene.remove_at(1)  # remove ExtResource if it's present

    scene.remove_at(1)      # remove Image
    scene.remove_at(1)      # remove ImageTexture

    ext = scene.add_ext_resource(texture, "Texture")
    resource = scene.find_section("resource")
    resource['albedo_texture'] = ext.reference

    scene.write(path)
    print(path)


if __name__ == '__main__':
    #path = Path(argv[1]).resolve()
    path = Path("D:\\Zeffyr\\assets\\Art Objects").resolve()
    if not path.is_dir():
        print("Not a directory ...")
        exit(-1)

    mp.freeze_support()
    files = list(path.glob("*.tres"))
    with mp.Pool() as pool:
        results = pool.map(edit_tres, files, chunksize=1)