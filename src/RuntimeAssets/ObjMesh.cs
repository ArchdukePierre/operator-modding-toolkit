using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace RuntimeAssets
{
    // Wavefront OBJ -> Unity meshes, built in engine at runtime.
    //
    // This exists because some IL2CPP builds refuse AssetBundles outright, which leaves runtime construction
    // as the only way to get custom geometry in. Each OBJ group becomes its own GameObject with a MeshFilter
    // and MeshRenderer, so parts can be textured, hidden or moved independently.
    //
    // Two details are load bearing. Vertex data crosses the interop boundary as Il2CppStructArray filled
    // element by element, because handing over a managed array can lose it to the collector mid call. And
    // OBJ is right handed while Unity is left handed, so a raw import is mirrored; see MirrorX below.
    public static class ObjMesh
    {
        public sealed class Options
        {
            public float Scale = 1f;
            public Material Material;
            public bool AddColliders;                       // required if AI navigation must see the geometry
            public bool Recalculate;                        // recompute normals instead of using the file's
            public HashSet<string> SkipGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        public sealed class Result
        {
            public GameObject Root;
            public int Parts;
            public int Vertices;
        }

        // Negative X scale on the root flips a right handed OBJ into Unity's left handed space. Unity corrects
        // winding and normals for a negative determinant automatically, so this is cheaper and safer than
        // rewriting the index buffer.
        public static void MirrorX(GameObject root)
        {
            if (root == null) return;
            var s = root.transform.localScale;
            root.transform.localScale = new Vector3(-Mathf.Abs(s.x), s.y, s.z);
        }

        public static Result Build(string objPath, Transform parent, Options opt = null)
        {
            opt ??= new Options();
            var res = new Result();
            if (!File.Exists(objPath)) return res;

            var v = new List<Vector3>();
            var vt = new List<Vector2>();
            var vn = new List<Vector3>();
            var groups = new List<KeyValuePair<string, List<int[]>>>();
            List<int[]> cur = null;

            foreach (var raw in File.ReadLines(objPath))
            {
                var line = raw.Trim();
                if (line.Length < 2 || line[0] == '#') continue;
                var tok = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                switch (tok[0])
                {
                    case "v":  v.Add(new Vector3(F(tok[1]), F(tok[2]), F(tok[3]))); break;
                    case "vt": vt.Add(new Vector2(F(tok[1]), tok.Length > 2 ? F(tok[2]) : 0f)); break;
                    case "vn": vn.Add(new Vector3(F(tok[1]), F(tok[2]), F(tok[3]))); break;

                    case "g":
                    case "o":
                        cur = new List<int[]>();
                        groups.Add(new KeyValuePair<string, List<int[]>>(
                            tok.Length > 1 ? tok[1] : "part" + groups.Count, cur));
                        break;

                    case "f":
                        if (cur == null)
                        {
                            cur = new List<int[]>();
                            groups.Add(new KeyValuePair<string, List<int[]>>("root", cur));
                        }
                        // corners are v/vt/vn, one based, and may be negative meaning relative to the end
                        var corners = new int[tok.Length - 1][];
                        for (int i = 1; i < tok.Length; i++)
                        {
                            var idx = tok[i].Split('/');
                            corners[i - 1] = new[]
                            {
                                Idx(idx[0], v.Count),
                                idx.Length > 1 && idx[1].Length > 0 ? Idx(idx[1], vt.Count) : -1,
                                idx.Length > 2 && idx[2].Length > 0 ? Idx(idx[2], vn.Count) : -1
                            };
                        }
                        // fan triangulation handles quads and n-gons
                        for (int i = 2; i < corners.Length; i++)
                            cur.Add(new[]
                            {
                                corners[0][0],     corners[0][1],     corners[0][2],
                                corners[i - 1][0], corners[i - 1][1], corners[i - 1][2],
                                corners[i][0],     corners[i][1],     corners[i][2]
                            });
                        break;
                }
            }

            var root = new GameObject(Path.GetFileNameWithoutExtension(objPath));
            if (parent != null) root.transform.SetParent(parent, false);

            foreach (var kv in groups)
            {
                string name = kv.Key;
                var faces = kv.Value;
                if (faces.Count == 0) continue;
                if (opt.SkipGroups.Contains(name.Split('.')[0])) continue;

                // re-index each unique (v, vt, vn) tuple into a local vertex list
                var map = new Dictionary<long, int>();
                var lv = new List<Vector3>();
                var lu = new List<Vector2>();
                var ln = new List<Vector3>();
                var tris = new List<int>(faces.Count * 3);

                foreach (var f in faces)
                    for (int c = 0; c < 3; c++)
                    {
                        int a = f[c * 3], b = f[c * 3 + 1], n = f[c * 3 + 2];
                        long key = ((long)(a + 1) * 1000003L + (b + 1)) * 1000003L + (n + 1);
                        if (!map.TryGetValue(key, out int li))
                        {
                            li = lv.Count;
                            map[key] = li;
                            lv.Add(v[a] * opt.Scale);
                            lu.Add(b >= 0 && b < vt.Count ? vt[b] : Vector2.zero);
                            ln.Add(n >= 0 && n < vn.Count ? vn[n] : Vector3.up);
                        }
                        tris.Add(li);
                    }

                var mesh = new Mesh { name = name };
                mesh.vertices  = ToV3(lv);
                mesh.uv        = ToV2(lu);
                mesh.triangles = ToI(tris);
                if (opt.Recalculate) mesh.RecalculateNormals(); else mesh.normals = ToV3(ln);
                mesh.RecalculateBounds();

                var go = new GameObject(name);
                go.transform.SetParent(root.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                if (opt.Material != null) mr.sharedMaterial = opt.Material;
                if (opt.AddColliders) go.AddComponent<MeshCollider>().sharedMesh = mesh;

                res.Parts++;
                res.Vertices += lv.Count;
            }

            res.Root = res.Parts > 0 ? root : null;
            if (res.Parts == 0) UnityEngine.Object.Destroy(root);
            return res;
        }

        // Shaders are centralised in the player build, so resolving by name is the reliable way to get a
        // usable material at runtime. Fall back through common pipeline names.
        public static Material MakeMaterial(string preferred = "HDRP/Lit")
        {
            foreach (var n in new[] { preferred, "HDRP/Lit", "Universal Render Pipeline/Lit", "Standard" })
            {
                var sh = Shader.Find(n);
                if (sh != null) return new Material(sh);
            }
            return null;
        }

        // ImageConversion needs the bytes as an il2cpp array, filled element by element for the same
        // collector reason as the mesh buffers.
        public static Texture2D LoadTexture(string path, bool linear = false)
        {
            try
            {
                if (!File.Exists(path)) return null;
                var bytes = File.ReadAllBytes(path);
                var arr = new Il2CppStructArray<byte>(bytes.Length);
                for (int i = 0; i < bytes.Length; i++) arr[i] = bytes[i];
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, true, linear);
                return ImageConversion.LoadImage(tex, arr) ? tex : null;
            }
            catch { return null; }
        }

        static float F(string s) => float.Parse(s, CultureInfo.InvariantCulture);

        static int Idx(string s, int count)
        {
            int i = int.Parse(s, CultureInfo.InvariantCulture);
            return i > 0 ? i - 1 : count + i;
        }

        static Il2CppStructArray<Vector3> ToV3(List<Vector3> src)
        { var a = new Il2CppStructArray<Vector3>(src.Count); for (int i = 0; i < src.Count; i++) a[i] = src[i]; return a; }

        static Il2CppStructArray<Vector2> ToV2(List<Vector2> src)
        { var a = new Il2CppStructArray<Vector2>(src.Count); for (int i = 0; i < src.Count; i++) a[i] = src[i]; return a; }

        static Il2CppStructArray<int> ToI(List<int> src)
        { var a = new Il2CppStructArray<int>(src.Count); for (int i = 0; i < src.Count; i++) a[i] = src[i]; return a; }
    }
}
