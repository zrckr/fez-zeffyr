#!/usr/bin/env python3
# -*- coding: utf-8 -*-

import shutil, sys, os, glob, stat
from pathlib import Path, PurePath
from fnmatch import fnmatch, filter

def copytree(src, dst, symlinks=False, ignore=None):
    for item in os.listdir(src):
        s = os.path.join(src, item)
        d = os.path.join(dst, item)
        if os.path.isdir(s):
            shutil.copytree(s, d, symlinks, ignore)
        else:
            shutil.copy2(s, d)
        print("[Copy]", s, "->", d)

def include_patterns(*patterns):
    def _ignore_patterns(path, names):
        keep = set(name for pattern in patterns
                for name in filter(names, pattern))
        ignore = set(name for name in names
                if name not in keep and not os.path.isdir(os.path.join(path, name)))
        return ignore
    return _ignore_patterns

def move_contents_to_parent(path):
    for p in path.iterdir():
        new = Path(path.parent, p.name)
        p.replace(new)

ROOT_FOLDERS = [
    "art objects",
    "background planes",
    "character animations",
    "music",
    "other textures",
    "skies",
    "sounds",
    "trile sets"
]

EXTS = [".png", ".wav"]

if len(sys.argv) != 2:
    print("The contents folder is not specified!")
    exit(-1)

# Debug string
# src = Path("C:\\Users\\User\\Desktop\\workspace\\Fez Assets").resolve()

src = Path(sys.argv[1]).resolve()
dst = PurePath.joinpath(Path(src).parent, "assets")
files = [p for p in Path(src).rglob('*') if p.suffix in EXTS]

print("* Source folder:", src)
print("* Dest folder:", dst)
print("* Number of files:", len(files))
input("Press any key to continue...")

if (os.path.exists(dst)):
    print("[Delete]", dst)
    try:
        shutil.rmtree(dst)
    except:
        print(f"Error occured! Delete the folder at {dst}")
        exit(-1)

exts2 = ['*'+e for e in EXTS]

print("[Create]", dst)
copytree(src, dst, ignore=include_patterns(*exts2))

for d in Path(dst).iterdir():
    if d.is_dir() and not d.name.lower() in ROOT_FOLDERS:
        print("[Delete]", d.name)
        shutil.rmtree(str(d))
    
    elif d.is_file():
        print("[Delete]", d.name)
        d.unlink()
    
    else:
        new_name = "Trilesets" if (d.name == ROOT_FOLDERS[-1]) else d.name.title()
        print("[Rename]", d.name, "->", new_name)
        d.rename(Path(d.parent, new_name))

for p in Path(dst).rglob('*'):
    if "_alpha" in p.name:
        p.unlink()
        print("[Delete]", p.name)
        continue
    
    if p.parent.name in ["Character Animations", "Sounds"] and p.is_dir():
        new_path = Path(p.parent, p.name.title().replace("_", " "))
        print("[Rename]", p.name, "->", new_path.name)
        p.rename(new_path)

    if (p.stem == "drums"):
        move_contents_to_parent(p)
        p.rmdir()

print('[Done]', "Now copy 'assets' folder to the root of the Zeffyr project")