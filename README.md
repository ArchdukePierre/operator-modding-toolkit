# OPERATOR Modding Toolkit

Reverse-engineering references and tooling for modding **OPERATOR** (Unity 6 IL2CPP, HDRP, Steam appid 1913370) with MelonLoader.

The game ships no SDK, no Workshop and no documentation. Everything here was derived by disassembling the compiled game code, reading the generated interop assemblies, opening every shipped asset bundle, and checking the result against the running game.

Two documents carry the weight. Everything else in this repository exists to support them.

---

## The architecture reference

**[`reference/operator-architecture.md`](reference/operator-architecture.md)** is the code teardown. 160 KB covering ten subsystems, four end-to-end execution traces with RVA offsets and per-step network authority, the list of hook targets that compile and never run, two dozen bugs found in the disassembly, and a seam table.

It exists because most of the time lost on this game goes to approaches that look correct and do nothing. A method with `CMD_` in its name may not cross the wire. A class named `V2` may be a corpse while the plain-named one ships. `Health` has no `Update`, because `GameManager` ticks it externally through a `CallUpdate` convention.

The trust posture is the headline finding. `WeaponV3.UserCode_CMD_SendBullet` and `WeaponV3.RPC_SendBullet` share one RVA because the linker folded them: the server handler is a verbatim rebroadcast with zero validation. Damage resolution is gated on a single byte, `BulletInstance.isOwner`, so the shooter's client decides which body part was hit and what the final number is. `Health.CMDTakeDamage` is `[Command(requiresAuthority = false)]`. Anything you build has to be designed around that.

Produced by ten parallel investigations, one per subsystem, then four cross-cutting traces following a single flow across subsystem boundaries: one shot from trigger to corpse, boot to standing in a mission, one bot from spawn to death, and one frame. Every load-bearing claim then went through an adversarial pass whose job was to break it. Of 232 claims checked, 176 held, 51 were overstated and got downgraded to inference, and 5 were wrong and were dropped.

Game code lives in a PE section named `il2cpp`, not `.text`. Where the reference cites an address, it came from disassembly. Where it says "likely" or "needs-runtime", it did not.

## The texture map

**[`reference/operator-textures.md`](reference/operator-textures.md)** is the art teardown, for retextures, camouflage, patches, reticles and reskins. 286 KB organising every texture and material into a tree by category, item and colourway, explaining the mechanism behind each one before giving the procedure to change it.

Built from a sweep of all 4,989 shipped bundles, read end to end with no errors:

```
4,989   bundles
7,247   Texture2D objects
2,603   materials
8,252   texture to shader-property bindings that reference a real texture
44,144  binding slots in total, the rest declared and empty
```

The mechanism, which is not what most people assume. A colourway is not an index and not a tint on a shared material. It is a separate material asset in its own bundle whose only meaningful difference from its siblings is `_BaseColorMap`. Mask and normal maps are shared in content, but Addressables duplicates them into every sibling bundle, so a texture name appearing in six bundles means six physical copies rather than one file to edit. Authoring a new camouflage pattern is therefore one 2048x2048 BC1 albedo per garment, and swapping a pattern across a family is one bundle edit per member.

The second mechanism is worth knowing before you go looking for a texture that is not there. The plain T-shirt colours bind no albedo at all and carry their colour in the `_BaseColor` float4.

Also covered: the split between a stripped prefab bundle and a material bundle and the pointer that joins them, the character and weapon duality that means a magazine exists twice with different materials, HDRP mask channel packing, the shader census and which properties you can drive, the standalone camouflage pattern library and what consumes each entry, patches and reticles as the two most approachable starting projects, and the traps that make a correct-looking bundle edit stop working after a game patch.

Ten parallel investigations produced 125 findings, 104 of them proven by opening bundles rather than inferred from names. Each section then went through its own adversarial accuracy pass against the raw scan. Every one came back with corrections.

---

## Everything else

`tools/BundleScan` is the scanner behind the texture map. Point it at a directory of AssetBundles and it writes a texture and material inventory as TSV. The useful output is `texenvs.tsv`, which records which texture is bound to which shader property on which material, because that mapping is what you need before you can replace anything. Not OPERATOR specific.

`tools/InteropInspector` dumps every type, method signature and field from a directory of managed assemblies, filtered by regex. Built on `System.Reflection.Metadata` so it never loads the assemblies, which makes it safe against the malformed and native DLLs sitting alongside the interop output. Not OPERATOR specific either, and it works against any Il2CppInterop game.

`docs/il2cpp-field-notes.md` is the list of traps. Read it before writing any code.

`docs/operator-api.md` is the API surface: weapons, character customisation, missions, infiltration, doors and AI navigation, with exact member names and signatures.

`template/` is a minimal MelonMod with a working csproj and a deploy script, so a first mod takes ten minutes instead of an afternoon.

`src/RuntimeAssets` is the runtime asset pipeline. AssetBundles can be made to load but need a raw il2cpp invoke and lose their materials, so building geometry and audio in engine at runtime is often the shorter path. `ObjMesh` parses Wavefront OBJ into Unity meshes with per group submeshes and correct index rewinding. `WavAudio` parses RIFF into an `AudioClip` without a decoder. Both are written against Il2CppInterop array marshalling, which is the part that usually breaks.

`data/` holds the raw research both references were built from. The architecture side is 240 subsystem findings and 168 traced pathway steps. The texture side is the full bundle scan: every texture with its dimensions and format, every material with its shader, every texture-to-property binding, the 35 material properties that carry authored variation, the generated tree in text and JSON, and 620 long-tail asset names resolved to the products they model. Claims carry a confidence field. Grep it when you need something a reference summarised away.

## Requirements

MelonLoader 0.7.3 or later. .NET SDK 8 or later for InteropInspector. Python 3 with UnityPy for BundleScan. A target build with generated interop assemblies under `MelonLoader/Il2CppAssemblies`.

## Using the tools

```powershell
cd tools/InteropInspector
.\run.ps1 "LoadoutManager|WeaponV3|GunStats" "C:\Path\To\Game\MelonLoader\Il2CppAssemblies"
```

Output is one line per member, pipe separated, in the form `assembly | type | kind | name | signature`. Broad patterns against 200 assemblies produce a lot, so anchor the regex when you can.

```bash
pip install UnityPy
python tools/BundleScan/scan.py "C:\Path\To\Game_Data\StreamingAssets\aa\StandaloneWindows64" out
```

Player build bundles have their Unity version stripped, so pass `--version` matching the build if it is not the 6000.3.8f1 default. Scanning OPERATOR's 4,989 bundles takes a few minutes. Progress is flushed every 200 bundles, so an interrupted run keeps what it already scanned.

## Scope

This repository contains no game assets. There is no pixel data, no mesh data, no audio, and no packed or repacked bundle from OPERATOR or any other title. Nothing here can be used to reconstruct one.

What `data/textures/` contains is metadata read out of the shipped bundles: asset names, image dimensions, compression formats, mip counts, shader names, which texture is bound to which shader property, and the numeric material properties. That is interface documentation of the same kind as the API reference, and it exists so that someone making their own art knows what to author against.

## Credits

Remero, visuals developer at ODG, corrected the AssetBundle section after loading one himself on the M107.

## License

MIT. See `LICENSE`.
