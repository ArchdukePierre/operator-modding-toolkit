#!/usr/bin/env python3
"""Sweep a directory of Unity AssetBundles and emit a texture and material inventory.

Written for OPERATOR's Addressables output, but nothing here is game specific. It reads every
bundle, walks the objects, and writes five tab separated files:

    textures.tsv   bundle, pathid, name, width, height, format, mips, streamed, bytes
    materials.tsv  bundle, pathid, name, shader_pathid, shader_name
    texenvs.tsv    bundle, material, prop, tex_pathid, tex_name, scale_x, scale_y, off_x, off_y
    props.tsv      bundle, material, kind, prop, value
    objects.tsv    bundle, size, kind, count
    errors.tsv     bundle, stage, error

texenvs.tsv is the useful one. It tells you which texture is bound to which shader property on
which material, which is what you need before you can replace anything.

Output is flushed every 200 bundles, so an interrupted run keeps what it already scanned.

Usage:
    pip install UnityPy
    python scan.py <bundle-dir> [out-dir] [--version 6000.3.8f1]

Player build bundles are version stripped, so pass the Unity version the game was built with.
You can read it from <Game>_Data/globalgamemanagers, or from the crash handler's version string.
"""
import argparse, collections, glob, os, sys, warnings

try:
    import UnityPy
except ImportError:
    sys.exit("UnityPy is not installed. Run: pip install UnityPy")

# Unity's TextureFormat enum, the values you actually meet in a shipped game.
TEXFMT = {
    1: "Alpha8", 2: "ARGB4444", 3: "RGB24", 4: "RGBA32", 5: "ARGB32", 7: "RGB565", 9: "R16",
    10: "DXT1", 12: "DXT5", 13: "RGBA4444", 14: "BGRA32", 15: "RHalf", 16: "RGHalf",
    17: "RGBAHalf", 18: "RFloat", 19: "RGFloat", 20: "RGBAFloat", 21: "YUY2", 22: "RGB9e5Float",
    24: "BC6H", 25: "BC7", 26: "BC4", 27: "BC5", 28: "DXT1Crunched", 29: "DXT5Crunched",
    41: "EAC_R", 42: "EAC_R_SIGNED", 43: "EAC_RG", 44: "EAC_RG_SIGNED", 47: "ETC2_RGBA8",
    48: "ASTC_4x4", 62: "RG16", 63: "R8",
}


def name_of(obj, default=""):
    v = getattr(obj, "m_Name", None)
    return v if isinstance(v, str) else default


def pairs(coll):
    """UnityPy hands back these maps as a dict, a list of 2-tuples, or a list of first/second
    objects depending on version and typetree. Normalise all three."""
    if coll is None:
        return
    items = coll.items() if hasattr(coll, "items") else coll
    for entry in items:
        if isinstance(entry, (tuple, list)) and len(entry) == 2:
            key, val = entry
        else:
            key, val = getattr(entry, "first", None), getattr(entry, "second", None)
        if not isinstance(key, str):
            key = getattr(key, "name", None) or str(key)
        yield key, val


def texenvs(mat):
    saved = getattr(mat, "m_SavedProperties", None)
    for key, val in pairs(getattr(saved, "m_TexEnvs", None) if saved else None):
        tex = getattr(val, "m_Texture", None)
        scale, off = getattr(val, "m_Scale", None), getattr(val, "m_Offset", None)
        yield (key,
               getattr(tex, "m_PathID", 0) if tex is not None else 0,
               getattr(scale, "x", 1.0) if scale is not None else 1.0,
               getattr(scale, "y", 1.0) if scale is not None else 1.0,
               getattr(off, "x", 0.0) if off is not None else 0.0,
               getattr(off, "y", 0.0) if off is not None else 0.0)


def scalars(mat):
    saved = getattr(mat, "m_SavedProperties", None)
    if saved is None:
        return
    for attr, kind in (("m_Floats", "f"), ("m_Ints", "i"), ("m_Colors", "c")):
        for key, val in pairs(getattr(saved, attr, None)):
            if kind == "c":
                val = "%.4f,%.4f,%.4f,%.4f" % (getattr(val, "r", 0), getattr(val, "g", 0),
                                               getattr(val, "b", 0), getattr(val, "a", 0))
            yield kind, key, val


def main():
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("bundles", help="directory holding the .bundle files")
    ap.add_argument("out", nargs="?", default="bundlescan", help="output directory")
    ap.add_argument("--version", default="6000.3.8f1",
                    help="Unity version to assume for version-stripped bundles")
    ap.add_argument("--glob", default="*.bundle", help="filename pattern (default *.bundle)")
    args = ap.parse_args()

    UnityPy.config.FALLBACK_UNITY_VERSION = args.version
    warnings.filterwarnings("ignore")
    os.makedirs(args.out, exist_ok=True)

    files = sorted(glob.glob(os.path.join(args.bundles, args.glob)))
    if not files:
        sys.exit(f"no files matching {args.glob} in {args.bundles}")
    print(f"{len(files)} bundles, assuming Unity {args.version}", flush=True)

    def openf(n, header):
        f = open(os.path.join(args.out, n), "w", encoding="utf-8", newline="\n")
        f.write(header + "\n")
        return f

    ftex = openf("textures.tsv",  "bundle\tpathid\tname\twidth\theight\tformat\tmips\tstreamed\tbytes")
    fmat = openf("materials.tsv", "bundle\tpathid\tname\tshader_pathid\tshader_name")
    fenv = openf("texenvs.tsv",   "bundle\tmaterial\tprop\ttex_pathid\ttex_name\tscale_x\tscale_y\toff_x\toff_y")
    fprp = openf("props.tsv",     "bundle\tmaterial\tkind\tprop\tvalue")
    fobj = openf("objects.tsv",   "bundle\tsize\tkind\tcount")
    ferr = openf("errors.tsv",    "bundle\tstage\terror")
    handles = (ftex, fmat, fenv, fprp, fobj, ferr)

    n_tex = n_mat = n_env = n_err = 0
    for i, path in enumerate(files):
        bundle = os.path.basename(path)
        try:
            objects = list(UnityPy.load(path).objects)
        except Exception as e:
            n_err += 1
            ferr.write(f"{bundle}\tload\t{type(e).__name__}: {e}\n")
            continue

        kinds = collections.Counter()
        tex_names, shader_names, materials = {}, {}, []

        for obj in objects:
            kind = str(obj.type.name)
            kinds[kind] += 1
            try:
                if kind == "Texture2D":
                    d = obj.read()
                    stream = getattr(d, "m_StreamData", None)
                    streamed = 1 if (stream is not None and getattr(stream, "path", "")) else 0
                    size = getattr(d, "m_CompleteImageSize", 0) or (getattr(stream, "size", 0) if stream else 0)
                    fmt = int(getattr(d, "m_TextureFormat", 0) or 0)
                    nm = name_of(d)
                    tex_names[obj.path_id] = nm
                    ftex.write(f"{bundle}\t{obj.path_id}\t{nm}\t{getattr(d,'m_Width',0)}\t"
                               f"{getattr(d,'m_Height',0)}\t{TEXFMT.get(fmt, fmt)}\t"
                               f"{getattr(d,'m_MipCount',0)}\t{streamed}\t{size}\n")
                    n_tex += 1
                elif kind == "Shader":
                    d = obj.read()
                    shader_names[obj.path_id] = (
                        name_of(d) or getattr(getattr(d, "m_ParsedForm", None), "m_Name", ""))
                elif kind == "Material":
                    materials.append(obj)
            except Exception as e:
                n_err += 1
                ferr.write(f"{bundle}\t{kind}\t{type(e).__name__}: {e}\n")

        # Materials are read last so shader and texture names in the same bundle are already known.
        for obj in materials:
            try:
                d = obj.read()
                mn = name_of(d)
                shader = getattr(d, "m_Shader", None)
                spid = getattr(shader, "m_PathID", 0) if shader is not None else 0
                fmat.write(f"{bundle}\t{obj.path_id}\t{mn}\t{spid}\t{shader_names.get(spid,'')}\n")
                n_mat += 1
                for prop, pid, sx, sy, ox, oy in texenvs(d):
                    fenv.write(f"{bundle}\t{mn}\t{prop}\t{pid}\t{tex_names.get(pid,'')}"
                               f"\t{sx}\t{sy}\t{ox}\t{oy}\n")
                    n_env += 1
                for k, prop, val in scalars(d):
                    fprp.write(f"{bundle}\t{mn}\t{k}\t{prop}\t{val}\n")
            except Exception as e:
                n_err += 1
                ferr.write(f"{bundle}\tMaterial\t{type(e).__name__}: {e}\n")

        size = os.path.getsize(path)
        for k, c in kinds.items():
            fobj.write(f"{bundle}\t{size}\t{k}\t{c}\n")

        if i % 200 == 0:
            for h in handles:
                h.flush()
            print(f"[{i}/{len(files)}] textures={n_tex} materials={n_mat} "
                  f"bindings={n_env} errors={n_err}", flush=True)

    for h in handles:
        h.close()
    print(f"done. {len(files)} bundles, {n_tex} textures, {n_mat} materials, "
          f"{n_env} bindings, {n_err} errors -> {args.out}", flush=True)


if __name__ == "__main__":
    main()
