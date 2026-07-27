# The OPERATOR texture map

Every texture and material in OPERATOR, organised into a tree by category, item and colourway, with
the mechanism behind each one and the procedure to change it.

This is the companion to `operator-architecture.md`. That document covers code and behaviour. This one
covers art, and is aimed at retextures, custom camouflage, patches, reticles and reskins.

## How this was produced

Every one of the 4,989 bundles the game ships was opened and walked with UnityPy, with no read errors.
That sweep is the factual base of this document and it is committed alongside it in `data/textures/`.

```
4,989   bundles read
7,247   Texture2D objects
2,603   materials
8,252   texture to shader-property bindings that reference a real texture
44,144  binding slots in total, the rest being declared and empty
397,568 material property values, of which 35 properties carry authored variation
```

On top of that base, eleven parallel investigations went after one area each, unpacking individual
bundles to answer questions the aggregate could not. Each then went through a separate pass whose job
was to break it: check every number against the raw scan, downgrade anything asserted beyond the
evidence, and cut anything unverifiable.

Where this document states a fact, it came from reading a bundle or querying the scan. Where it says
the naming implies, or marks something unverified, it did not. Runtime behaviour is called out as
needing a probe rather than presented as established.

The category trees were built by regex over the asset names for the clear cases, then the 620 names
that did not classify were resolved individually against real-world product knowledge. That mapping is
in `data/textures/item-products.json`, so you can check it and correct it.

## Contents

1. [How the art pipeline is put together](#1-how-the-art-pipeline-is-put-together)
2. [Texture anatomy](#2-texture-anatomy)
3. [Shaders and their properties](#3-shaders-and-their-properties)
4. [Camouflage and colourways](#4-camouflage-and-colourways)
5. [The character gear tree](#5-the-character-gear-tree)
6. [The weapon attachment tree](#6-the-weapon-attachment-tree)
7. [Patches, insignia, optics and reticles](#7-patches-insignia-optics-and-reticles)
8. [World, effects and interface art](#8-world-effects-and-interface-art)
9. [Editing a bundle, and the traps](#9-editing-a-bundle-and-the-traps)

## 1. How the art pipeline is put together

This reference maps the shipped art of OPERATOR: every texture, every material, the bundle each one lives in, and which file you edit to change a given thing on screen. It is a lookup document, not a tutorial. The game is Unity 6000.3.8f1, IL2CPP, HDRP, Steam appid 1913370, and the live art sits in `D:\Games\OPERATOR\OPERATOR_Data\StreamingAssets\aa\StandaloneWindows64`.

It was produced by opening all 4,989 live bundles with UnityPy 1.25.2 and reading their object tables end to end, with zero read errors. That sweep yielded 7,247 `Texture2D` objects, 2,603 `Material` objects, 44,144 texture-property records and 397,568 material scalar and colour values, all banked as TSVs alongside this document. Every count here is computed from that sweep or from a follow-up pass over the same folder, never estimated. Targeted unpacks filled in the parts a bulk sweep cannot see, notably prefab component payloads and renderer material slots. Where a claim rests on inference rather than a read, the sentence says so.

The folder holds 4,990 files, not 4,989. The extra one is a stray `Patch_Select_Russia.jpg` sitting loose next to the bundles. Filter on `*.bundle`, not on `*.*`.

The bundles are version stripped, so any tool you point at them needs the version supplied manually:

```python
import UnityPy
UnityPy.config.FALLBACK_UNITY_VERSION = "6000.3.8f1"
env = UnityPy.load(r"...\charactermodmaterials_assets_mi_crye_pants_m81_cb3ba44b14c5ac99e5a250a06c058387.bundle")
```

### The four families

Four families cover 4,974 of the 4,989 bundles and 12.30 GiB of the 12.31 GiB on disk. All sizes in this document are binary, 1024^3 to the GiB; the same library reads as 13.22 GB if your file manager counts in powers of ten. Object counts are from the full sweep, sizes are compressed on-disk bytes.

| Family | Bundles | On disk | Materials | Texture2D | What it holds |
|---|---:|---:|---:|---:|---|
| `weaponmods_stripped` | 1,581 | 0.43 GiB | 206 | 184 | Attachment prefabs. Meshes, components, LODs, reticle and IES stragglers. No finish. |
| `weaponmodmaterials` | 1,203 | 5.94 GiB | 1,204 | 3,641 | One weapon material per bundle plus its maps. |
| `charactermods_stripped` | 1,165 | 0.81 GiB | 155 | 314 | Gear prefabs. Meshes, components, a few stray swatch textures. |
| `charactermodmaterials` | 1,025 | 5.12 GiB | 1,036 | 3,103 | One gear material per bundle plus its maps. |

The remaining 15 are 12 localization bundles, a shared `monoscripts` bundle carrying 69 `MonoScript` objects, a `unitybuiltinassets` bundle holding the `Sprites-Default` material and its shader, and one `defaultlocalgroup` bundle. That last one is not filler and it is a live reskin target. `defaultlocalgroup_assets_mat_carryhandle_2034f6e90a7a07d4d2906186afbee036.bundle` is 8.81 MiB and holds the material `mat_carryhandle` on shader `HDRP/Autodesk Interactive/AutodeskInteractive`, bound to five 2048x2048 textures named `M4 Carryhandle_COL`, `_RGS`, `_MET`, `_AO` and `_NRM`, the first four DXT1 and the normal DXT5. It is the one shipped art bundle that uses split legacy maps instead of the HDRP mask triplet, and its container key is a bare GUID rather than an asset path. Edit it and you repaint the M4 carry handle.

The "one material per bundle" rule is close to absolute: 1,022 of 1,025 `charactermodmaterials` bundles contain exactly one `Material`, and 1,202 of 1,203 on the weapon side. The modal texture count is three, in 867 of 1,025 and 829 of 1,203 bundles respectively, which is the HDRP triplet. Of the 44,144 texture-property records, 8,252 actually carry a texture; counting only those, `_NormalMap` appears 2,076 times, `_BaseColorMap` 2,056 and `_MaskMap` 2,003. The gap is real: each of those three props is declared 2,282 times, so a couple of hundred materials leave a slot empty.

A further 24 `weaponmodmaterials` bundles ship no texture at all. They are a mixed bag, not one category. Six are `Shader Graphs/RealisticScopeEffect` render targets (`3xmag_rendertarget`, `acog_rendertarget`, `burris scope effect`, `G33_Rear_Glass_RT`, `newRSE`, `razorhdg2e16_reticle`), with one more on the non-dual-render variant and one `Shader Graphs/RealisticNVG 2` for `PVS30`. Four are glass and lens materials on `HDRP/Lit` (`G33_lens`, `glass`, `Lens`, `mag_Glass`, plus `P320_Optic_Glass` and `SRS_FrontGlass`). Two are lasers, `IR Laser` and the two `Vis Laser` variants. One is the shared placeholder. The rest are plain parts that carry their whole look in shader floats: `Barrel_10.5Inch`, `Buttstock_MOECarbine`, `UpperReciever_Standart`, `GLOCK17_FrontSight 1`, `scope_pso6x36 1` and `white`. Do not assume a textureless bundle is glass.

### Filename grammar

```
<family>_assets_<slug>_<32 hex content hash>.bundle

charactermodmaterials_assets_mi_crye_pants_m81_cb3ba44b14c5ac99e5a250a06c058387.bundle
└─ family ────────────┘        └─ slug ──────┘ └─ content hash ──────────────┘
```

The slug is the asset name lowercased with separators removed. It is unique inside its family: all 1,025 `charactermodmaterials` slugs are distinct, as are all 1,203 `weaponmodmaterials`, all 1,165 `charactermods_stripped` and all 1,581 `weaponmods_stripped`. Across families 22 slugs collide, in three of the six possible pairings.

| Pairing | Colliding slugs | Examples |
|---|---:|---|
| `weaponmodmaterials` x `weaponmods_stripped` | 15 | `eotech553`, `eotech553fde`, `pvs30`, `ngal`, `handguard_agency`, `stock_rpk` |
| `charactermodmaterials` x `charactermods_stripped` | 5 | `bracelet`, `bracelet2`, `cryeg3aor2`, `cryeg3cutmcb`, `usa` |
| `charactermodmaterials` x `weaponmodmaterials` | 2 | `blk`, `body` |

The other three pairings are clean, including both stripped families against each other. The 12 localization bundles are the one break in the grammar, carrying no hash at all.

The 32 hex suffix is an Addressables build hash and nothing verifies it at runtime. The catalog is local only and holds the filename as a path string, and the per-bundle CRC field is zero, so a rebuilt bundle renamed to the current hash loads. The cost is that a game patch rewrites hashes in bulk: file mtimes show 1,108 bundles rewritten on 2026-07-06, 22.2% of the library in one day. As of this writing, 0 of 4,989 hashes have changed against the 2026-07-16 snapshot, so every name in this document is currently valid verbatim.

Do not hardcode a full filename in an install script, but do not glob naively either. A bare `<slug>_*.bundle` over-matches twice over. It crosses families for the 22 collisions above, and 73 slugs are a strict prefix of a longer sibling slug in the same family, so `mi_mpu5_*.bundle` also catches `mi_mpu5_blk` and `mi_crye_ls_*.bundle` also catches `mi_crye_ls_dts`. Store family plus slug and anchor the hash:

```sh
# safe: family-scoped, and the 32 hex is matched positionally
charactermodmaterials_assets_mi_mpu5_????????????????????????????????.bundle
```

That survives every patch that does not restructure the groups.

### One item, two bundles

Every item is split. Shape lives in a `*_stripped` prefab bundle, look lives in a `*materials` bundle, and in the common case the two are joined by an Addressables asset GUID rather than by a direct asset pointer. The Crye G3 pants trace:

```
charactermods_stripped_assets_cryeg3pantsm81_8ce8cd8d4058059965ed3f56967444e7.bundle   203 KiB
└── GameObject "CRYE G3 Pants M81"     key Assets/AddressableAssetsStripped/CharacterMods/CRYE G3 Pants M81.prefab
    ├── Mesh SKM_CRYE_Pants, Mesh CRYE_PADS            embedded, one copy per colourway
    ├── SkinnedMeshRenderer x2                         path ids 897632694866682161, 5789663925705056561
    │     m_Materials[0] = PPtr(m_FileID=2, m_PathID=-5343436609273763532)
    ├── MonoBehaviour  CharacterMod          AssetReferenceIndex 563, ModInfoIndex 563, modSlot 6,
    │                                        isSkinned 1, modifyColour 0, textureModifer 0,
    │                                        materialIndex 0, material null,
    │                                        materialBaseColourReference ""
    └── MonoBehaviour  AllMaterialReferences
          [{Renderer -> 897632694866682161,  Materials: [{m_AssetGUID "283c187b01d6b6a46998563462bc9954"}]},
           {Renderer -> 5789663925705056561, Materials: [{m_AssetGUID "283c187b01d6b6a46998563462bc9954"}]}]
                                                  │
                       Addressables catalog.bin   │  GUID present in the catalog string pool
                                                  ▼
charactermodmaterials_assets_mi_crye_pants_m81_cb3ba44b14c5ac99e5a250a06c058387.bundle  3.65 MiB
└── Material MI_CRYE_Pants_M81   shader Shader Graphs/ClothWind   m_Dependencies: []
    ├── _BaseColorMap  crye retexture_MI_CRYE_Pants_MC_BaseMap  2048x2048 DXT1  12 mips
    ├── _MaskMap       CryePantMask                             1024x1024 DXT5  11 mips
    └── _NormalMap     CRYE_G3_MC_Normal_12                     1024x1024 DXT5  11 mips
```

Note the shader. Garments in this family are `Shader Graphs/ClothWind`, not `HDRP/Lit`, so do not assume the HDRP property set on a cloth item without checking; the three map slots happen to be named identically here.

Correct one thing before you go further, because earlier community notes get it backwards. `m_FileID=2` does not point at the item's material bundle. `m_FileID` is a one-based index into that serialized file's own externals table, so the number itself means nothing across bundles; in this prefab the externals are `[CAB-62081254e5f53c3d16f8fcf3c684729f, CAB-5b00af9b95ebb9aec2086aa927439000]` and index 2 is the second of those. The path ID is the stable identifier. Resolved, `m_PathID=-5343436609273763532` lands in `weaponmodmaterials_assets_modplaceholdermaterial_26ab66620194ad9caab63da2f7a35e00.bundle`, inner file `cab-5b00af9b95ebb9aec2086aa927439000`, on a `Material` named `mod placeholder material` on `HDRP/Lit`, addressable as `Assets/AddressableAssetsStripped/mod placeholder material.mat`. That is almost certainly `LoadoutManager.PlaceholderMaterial`, which exists as a `UnityEngine.Material` field in the IL2CPP dump; the identification is by name and role, it is not proven from a runtime read.

The sentinel dominates. Across every one of the 1,165 `charactermods_stripped` bundles it fills 6,345 of 6,931 renderer material slots, with 243 bundle-internal pointers and 343 other externals; across all 1,581 `weaponmods_stripped` bundles, 3,773 of 4,620 slots, with 199 internal and 648 other. Those are full-population counts, not samples.

The bundle-level dependency list mostly agrees. The modal `m_Dependencies` set is exactly two CABs, the placeholder bundle and `5df1b7680c9342147f466a5ca660184a_monoscripts`, in 771 of 1,165 character prefabs and 1,339 of 1,581 weapon prefabs. It is not universal, so do not state it as a rule. 142 `charactermods_stripped` and 199 `weaponmods_stripped` bundles list at least one further material bundle, 146 distinct dependency targets in all; `weaponmodmaterials_assets_barrel_10_...` and `weaponmodmaterials_assets_m4x_barrel_370_...` are each a static dependency of 73 weapon prefabs. What holds for the Crye pants and for every simple colourway item is narrower: the colourway material bundle is not among the prefab's dependencies. On the other side the rule is nearly clean, 1,022 of 1,025 `charactermodmaterials` and 1,194 of 1,203 `weaponmodmaterials` bundles have `m_Dependencies: []`, and the 12 exceptions depend only on the monoscripts CAB. No material bundle depends on another material bundle.

So the real edge for a colourway is the GUID in `AllMaterialReferences`, one per renderer, resolved through `catalog.bin` at load. Which component consumes it is unverified. `AllMaterialReferences` appears in `identifiers.txt` but no dumped type declares it, and `Il2Cpp.CharacterMod` exposes a single `material` and a single `rendToModify` against a per-renderer list, so the private `CharacterMod.ActuallySetMaterial()` is a plausible but unconfirmed consumer. Needs a runtime probe. The data side is proven: all 20 Crye G3 pants prefabs carry exactly one material GUID each and all 20 are distinct, one per colourway, and the M81 GUID `283c187b01d6b6a46998563462bc9954` appears in the catalog string pool.

Three consequences follow, and they set the shape of every job in this document.

A reskin touches the material bundle and nothing else. The GUID lives in the prefab, the texture bytes live in the material bundle, and replacing the bytes leaves the GUID and the catalog untouched. You never repack a `*_stripped` bundle for a reskin, which also means you cannot break a mesh or a component while doing one.

You can repaint an existing option, but you cannot add a new one by dropping in a file. A brand-new bundle mints a GUID that is not in `catalog.bin`, and renaming to a hash does not register it. Adding a selectable colourway needs runtime code, not a file swap.

One material bundle edit changes every prefab that references that GUID. That works for you when a mask or normal is shared across sixteen camo siblings, and it is the classic wrong-copy accident when a texture set is shared between a vest and a helmet.

### charactermod versus weaponmod: the PMAG

The two families are not two halves of one catalogue. They are two independent art pipelines that happen to model the same objects, and the naming conventions do not meet. The character side favours product names with an `MI_` material-instance prefix, 598 of 1,025 slugs; the weapon side keeps the raw source-mesh name, with only 79 of 1,203 prefixed. There is no shared token to join them on, so you cannot script the pairing.

The Magpul PMAG is the case that catches people. The same magazine exists in the chest rig and in the rifle, and the two are unrelated files. Searching the live folder for `pmag` returns 33 bundles, split 6 `charactermodmaterials`, 10 `weaponmodmaterials`, 17 `weaponmods_stripped`, and zero `charactermods_stripped`. The character side ships magazine materials but no magazine prefabs of its own, which is consistent with the proven finding that rig pouches are baked geometry inside the carrier prefab rather than separate items; the specific mapping from a rig magazine mesh to its material has not been traced end to end.

Take the PMAG GEN3 M3, one real product, two worlds:

```
IN THE GUN     weaponmodmaterials_assets_magazine_pmag30_genm3_56029a0e128277afccea0b3b17f82533
               Material magazine_pmag30_genM3          shader HDRP/Lit
               _BaseColorMap  magazine_pmag30_genM3_AlbedoTransparency_black   2048 DXT1
               _MainTex       magazine_pmag30_genM3_AlbedoTransparency_black   2048 DXT1  (same texture)
               _MaskMap       magazine_pmag30_genM3_MaskMap                    2048 DXT5
               _NormalMap     magazine_pmag30_genM3_Normal                     2048 DXT5

ON THE RIG     charactermodmaterials_assets_mi_mag_magpul_pmag_gen3_m3_556_30_blk_4b0e19479cacdf4a1968edd91e61c928
               Material MI_Mag_Magpul_PMAG_GEN3_M3_556_30_BLK   shader MilkShaders/Lit-Template
               _BaseColorMap  Mag_Magpul_PMAG_GEN3_M3_556_30_BLK_BaseColor     2048 DXT1
               _MaskMap       MaskMap                                          2048 DXT5
               _NormalMap     Mag_Magpul_PMAG_GEN3_M3_556_30_Camo_DirectX_Normal 2048 DXT5
```

Different material name, different shader, different texture names, different UVs. Edit the first and the loaded magazine changes while every pouch magazine stays stock. Edit the second and the reverse happens. The divergence goes further than one pair: a second character-side PMAG, `charactermodmaterials_assets_mi_pmag_556_e6388d7446e5ff293e3a55fcdeea5258`, is `HDRP/Lit` with `PMAG_Black_BaseColor` and `PMAG_Black_Normal` at 1024x1024, half the linear resolution and a quarter of the texels of its sibling on the same physical object. Its `_MaskMap` stays at 2048, and it is DXT1 rather than DXT5, so only albedo and normal drop.

The colourway seam on the weapon side is visible in the GUID lists. `weaponmods_stripped_assets_556x45pmag_c8a5f05b3d9730fd61dd0b7f388bd329` binds three material GUIDs, `771cd65a0d5c2254f86221cae2e5b31d` twice and `c1091409a38a2334389b81d110f10840` once. Its FDE twin, `weaponmods_stripped_assets_556x45pmagfde_0ddf1734ae4a85a2ceacd321753385f8`, binds the same `771cd65a…` twice and swaps the third for `8d8f3640349ddae4e8b578efadc13086`. One GUID is the whole difference between black and FDE.

Practical rule for the rest of this document: before starting any magazine, mount, sling or shared-hardware job, search both material families and expect the names not to match. Anchor magazine searches on `pmag`, `magazine` or `_mag_` with delimiters, because a bare `mag` in `charactermods_stripped` returns five Gatorz Magnum eyewear tint variants and no magazines at all.

## 2. Texture anatomy

The shipped art is 7,247 `Texture2D` objects spread across 2,514 of the 4,989 live bundles, totalling 18.75 GiB of decoded texture payload packed into 12.31 GiB of `.bundle` files on disk. Every bundle sampled for a compression flag (200 of them, header read directly) is UnityFS format 8 with compression mode 3, LZ4HC. Every number below is computed from `textures.tsv`, `texenvs.tsv`, `materials.tsv` and `props.tsv`, the UnityPy sweep of all 4,989 bundles with zero read errors, plus targeted re-probes of individual bundles where pixel data, colour space or the shader property block was needed.

### 2.1 Format

Eight formats appear. Two of them cover 96.11% of the library.

| Format | Count | Share | Payload | Bytes per base pixel | Role |
|---|---:|---:|---:|---:|---|
| DXT5 (BC3) | 4,143 | 57.17% | 12.837 GiB | 1.330 | normals, mask maps, most reticles |
| DXT1 (BC1) | 2,822 | 38.94% | 5.464 GiB | 0.666 | base colour, ORM, greyscale PBR |
| BC7 | 216 | 2.98% | 0.414 GiB | 1.333 | 92 normals, 76 Autodesk-to-HDRP masks, 48 scope and rail maps |
| RGB9e5Float | 44 | 0.61% | 0.003 GiB | 4.000 | 2D IES light cookies |
| RGBA32 | 18 | 0.25% | 0.021 GiB | 4.560 | 11 reticles, 5 Garmin GPS glyphs, 2 Gen5 rear sight maps |
| Alpha8 | 2 | 0.03% | 0.002 GiB | 1.000 | TextMeshPro SDF atlases |
| BC6H | 1 | 0.01% | 0.0003 GiB | 1.333 | `T_EyeMidPlaneDisplacement` |
| RGB24 | 1 | 0.01% | 0.004 GiB | 4.000 | `Gen5_RearSight_low_Gen5_Rear_Sight_BaseMap` |

There is no BC4 and no BC5 anywhere in the game. That matters more than it sounds: BC5 is the normal-map format most authoring pipelines default to, and using it here puts your texture in a format class the rest of the library never uses.

The bytes-per-base-pixel column is total payload divided by the sum of base-level pixels. DXT1 is 0.5 bytes per pixel and DXT5 and BC7 are 1.0; multiply by the 4/3 mip-chain overhead and you get 0.667 and 1.333, which is what the data shows. DXT5 lands slightly under at 1.330 because 44 of its members carry no mip chain. Nothing in the library is padded beyond block alignment.

BC7 is not an upgrade path, it is a small set of exceptions. 92 of the 216 are normal maps, 76 are `*_MaskMap_AutodeskToHDRP` (21 distinct names duplicated across bundles; the name implies Unity's Autodesk Interactive material upgrader produced them, which the bundles themselves do not prove), and the remaining 48 are scope, rail and glove maps. Authoring a BC7 replacement renders fine but doubles that texture's byte cost against its DXT1 neighbours and changes the bundle footprint.

### 2.2 Resolution

| Resolution | Count | Share |
|---|---:|---:|
| 2048x2048 | 3,971 | 54.80% |
| 1024x1024 | 1,963 | 27.09% |
| 512x512 | 866 | 11.95% |
| 2048x1024 | 118 | 1.63% |
| 256x256 | 82 | 1.13% |
| 32x32 | 63 | 0.87% |
| 128x128 | 58 | 0.80% |
| 1024x512 | 33 | 0.46% |
| 64x64 | 18 | 0.25% |
| 4096x4096 | 17 | 0.23% |
| 256x512 | 15 | 0.21% |
| 1000x1000 | 15 | 0.21% |
| 4x4 | 14 | 0.19% |
| 512x256 | 7 | 0.10% |
| 21x32, 128x85, 27x32, 32x26, 256x128, 16x16 | 1 each (2 for 21x32) | 0.10% |

2048x2048 is the house standard and 93.83% of the library sits at 512, 1024 or 2048 square. The 1000x1000 group is 15 copies of one texture named `Reticle03`, which is why it is not a power of two. The 4x4 group is shipped art, not a scan artefact: all 14 are DXT1, all 14 are bound to `_MaskMap`, and they include `M_Eye_MaskMap` on the `M_Eye` material plus `PMAG_MaskMap`, `Reflex_MaskMap` and six others.

### 2.3 Mip chains

Mip counts are binary in practice. Either the texture carries a complete chain down to 1x1, or it carries exactly one level.

| Mip count | Textures | Longest side |
|---:|---:|---|
| 13 | 14 | 4096 |
| 12 | 4,075 | 2048 (3,957 at 2048x2048, 118 at 2048x1024) |
| 11 | 1,984 | 1024 (1,951 at 1024x1024, 33 at 1024x512) |
| 10 | 857 | 512 (835 at 512x512, 15 at 256x512, 7 at 512x256) |
| 9 | 83 | 256 (82 at 256x256, 1 at 256x128) |
| 8 | 11 | 128 |
| 7 | 18 | 64 |
| 6 | 62 | 32 |
| 5 | 1 | 16 |
| 3 | 14 | 4 |
| 1 | 128 | no chain |

7,119 of 7,247 textures (98.23%) carry `m_MipCount == floor(log2(max(w,h))) + 1`, a complete chain. The remaining 128 all have `m_MipCount == 1`. There is not a single partially truncated chain in the game, so "does it have mips" is a yes/no question rather than a count you have to match level by level.

The 128 single-level textures break down as 47 at 128x128, 31 at 512x512, 15 at 1000x1000, 14 at 2048x2048, 12 at 1024x1024, 3 at 4096x4096 and 6 at odd sizes. 75 are 256 px or larger and 29 are 1024 px or larger. Most of that large group is deliberate: roughly 16 of the 29 are reticles, including the three 4096x4096 copies of `Vortex HD Gen 3 Reticle2`, and a scope samples a reticle near 1:1. Two more are TextMeshPro SDF atlases, which must not be mipped.

That leaves ten large single-mip textures that read as defects. Six are patches, `Patch_US_IR_BaseColor` at 2048x2048 DXT1 with one mip in three separate bundles plus `Patch_US_BW_BaseColor`, `Patch_JTAC_BaseColor` and `Patch_Golden_SQ_BaseColor`. Four are Crye garment albedos:

```
mi_crye_pants_aor1worn   cryeg2g3_MI_CRYE_Pants_MC_BaseMap        1024  mips=1   streamed=0
mi_crye_pants_dts        crye retexture_MI_CRYE_Pants_MC_BaseMap  1024  mips=1   streamed=0
mi_crye_ls_dts           crye retexture_MI_CRYE_LS_BaseMap        1024  mips=1   streamed=0
mi_crye_ls_m81           crye retexture_MI_CRYE_LS_BaseMap        1024  mips=1   streamed=0
                         ... while 14 of the 16 pants bundles and 13 of the 16 LS
                         bundles ship a 2048 albedo with 12 mips, streamed
mi_crye_ls_aor1worn      cryeg2g3_MI_CRYE_LS_BaseMap              4096  mips=13  streamed=1
```

This extends the two-pants defect named in section 1 to the long-sleeve pair. `cryeg2g3_MI_CRYE_Pants_MC_BaseMap` is 1024 with one mip in `mi_crye_pants_aor1worn` and 2048 with twelve in `mi_crye_pants_mcworn` and `mi_crye_pants_mcbworn`. `cryeg2g3_MI_CRYE_LS_BaseMap` is 4096 in one bundle and 2048 in two others. Same texture name, up to four times the texels, different mip policy, different streaming flag. Never match a texture by name alone.

### 2.4 The streaming flag and the `.resS` block

7,191 of 7,247 textures (99.23%) have `m_StreamData` populated. For those, `image_data` inside the serialized `Texture2D` is zero bytes long and the pixels live in a sibling stream inside the bundle archive:

```
m_StreamData.path   archive:/CAB-d97ba4853237ce737984cae271243063/CAB-d97ba4853237ce737984cae271243063.resS
m_StreamData.offset 6291504
m_StreamData.size   5592432
```

Every bundle in a 60-bundle random sample contains exactly two inner files, `CAB-<hash>` (the serialized objects) and `CAB-<hash>.resS` (the raw block-compressed payload), including the bundles whose textures are all inline. The `Texture2D` object is a header with a pointer. Across those 60 bundles and 188 textures, all use the `archive:/CAB-` form, every offset is 16-byte aligned, and blocks are laid down back to back with 0 or 8 bytes of padding to reach that alignment (gap counts 77 and 51). Worked example from `charactermodmaterials_assets_anprc152_aor1_dacb9db194b593dae39108499cc0d344.bundle`:

```
LitMask                        2048 DXT5 mips=12   offset 0          size 5,592,432
JPC_V2_ANPRC152_sdr_BaseMap 3  1024 DXT1 mips=11   offset 5,592,432  size   699,064
ANPRC152_Normal                2048 DXT5 mips=12   offset 6,291,504  size 5,592,432
                                        5,592,432 + 699,064 = 6,291,496, padded to 6,291,504
```

The repack consequence is direct. A streamed texture's bytes are addressed by absolute offset and length into one shared blob, so changing any texture's size shifts every later block in that `.resS` and invalidates every `m_StreamData.offset` after it. If you hand-patch bytes and your replacement is a different size, or has a different mip count so the chain length changes, the loader reads the wrong range and you get a black or corrupt texture. Rebuild through a Unity project or a tool that rewrites the `.resS` and fixes up every offset, rather than splicing bytes, whenever the payload size changes at all.

The 56 non-streamed textures behave differently. Forty-four are `RGB9e5Float` 128x128 2D IES light cookies (`ScatterLight-2D-IES`, `ParallelBeam-2D-IES`, `XArrowDiffuse-2D-IES` and friends), 43 in `weaponmods_stripped` and one in `charactermods_stripped`. Of the remaining twelve, ten are the four Crye base maps and six patch base colours listed above; the last two are `Gen5_RearSight_low_Gen5_Rear_Sight_BaseMap` and `GLOCK17_Slide_Base_color`, both full-chain but inline. All twelve were re-opened and confirmed to carry their pixels in `image_data` with an empty `m_StreamData.path`. Streaming and mip count are independent flags: 74 of the 128 single-mip textures are still streamed.

### 2.5 Which map slots are actually bound

`texenvs.tsv` has 44,144 rows across 50 distinct property names, but a row only means the shader declares the slot. Only 8,257 rows carry a non-zero texture pointer, and 8,252 of those resolve to a texture in the same bundle. The five that do not are two `IR Strobe` materials binding `_BaseColorMap` and `_MainTex` to a pathid outside their bundle, plus `_SceneRender` on `Scope Material Base` in `weaponmodmaterials/lpvomaterial`.

| Property | Declared | Bound | Empty |
|---|---:|---:|---:|
| `_NormalMap` | 2,282 | 2,076 | 206 |
| `_BaseColorMap` | 2,282 | 2,058 | 224 |
| `_MaskMap` | 2,282 | 2,003 | 279 |
| `_MainTex` | 1,719 | 1,422 | 297 |
| `_SpecularColorMap` | 1,510 | 200 | 1,310 |
| `_BumpMap` | 118 | 116 | 2 |
| `_MetallicGlossMap` | 117 | 115 | 2 |
| `_SpecGlossMap` | 117 | 114 | 3 |
| `_Reticle` | 40 | 40 | 0 |
| `_EmissiveColorMap` | 2,036 | 35 | 2,001 |
| `_OcclusionMap` | 117 | 28 | 89 |
| `Reticle` | 31 | 20 | 11 |
| `_OpacityMap` | 109 | 10 | 99 |
| `_Emission` | 320 | 8 | 312 |
| `_DetailMap` | 1,952 | 6 | 1,946 |
| `_MeatBaseMap` / `_MeatNormalMap` | 2 / 2 | 2 / 2 | 0 |
| `_HeightMap` | 1,510 | 1 | 1,509 |
| `_SceneRender` | 1 | 1 | 0 |
| `unity_Lightmaps` / `unity_LightmapsInd` / `unity_ShadowMasks` | 2,507 each | 0 | 2,507 each |
| `_AnisotropyMap`, `_BentNormalMap`, `_CoatMaskMap`, `_IridescenceMaskMap`, `_SubsurfaceMaskMap`, `_TangentMap`, `_ThicknessMap`, `_TransmissionMaskMap`, `_TransmittanceColorMap`, `_NormalMapOS`, `_BentNormalMapOS`, `_TangentMapOS`, `_IridescenceThicknessMap` | 1,510 each | 0 | 1,510 each |
| `_EmissionMap` 117, `_Heli_Wind_Mask` 109, `_DistortionVectorMap` 80, `_UnlitColorMap` 80, `_Noise_Texture` 40, `_SmoothnessMask` 4, `_MeatMaskMap` 2, and six one-off slots | as listed | 0 | all |

The working surface is three slots. 1,968 materials bind the full `_BaseColorMap` + `_MaskMap` + `_NormalMap` trio: 1,192 on `HDRP/Lit`, 437 on `MilkShaders/Lit-Template`, 205 on `MilkShaders/UERemap`, 101 on `Shader Graphs/ClothWind`, 31 whose shader pointer does not resolve inside the bundle, and 2 on `GoreShader`. 2,282 materials declare that trio, across `HDRP/Lit` 1,314, `MilkShaders/Lit-Template` 439, `MilkShaders/UERemap` 207, unresolved 203, `ClothWind` 109, `HDRP/Hair` 4, `HDRP/Decal` 4 and `GoreShader` 2. Treat 2,282 as the count of art materials on a shader exposing the HDRP-style trio, not as a count of `HDRP/Lit` materials.

318 materials bind no texture at all. The largest group is 169 whose shader pointer does not resolve inside the bundle, then `HDRP/Unlit` 80, `Shader Graphs/RealisticNVG 2` 39 and `HDRP/Lit` 16. 279 materials declare `_MaskMap` and leave it empty; 99 of those are `HDRP/Lit`, and a 25-material sample of that 99 shows none of them carries the `_MASKMAP` keyword, so HDRP falls back to the scalar `_Metallic` and `_Smoothness`.

Format is not free-for-all per slot. The library is consistent enough that a deviation is a signal:

| Slot | Bindings | Format distribution |
|---|---:|---|
| `_BaseColorMap` | 2,056 | DXT1 1,914, DXT5 130, BC7 11, RGB24 1 |
| `_MainTex` | 1,420 | DXT1 1,294, DXT5 113, BC7 10, Alpha8 2, RGB24 1 |
| `_NormalMap` | 2,076 | DXT5 1,979, BC7 92, DXT1 4, RGBA32 1 |
| `_MaskMap` | 2,003 | DXT5 1,629, DXT1 276, BC7 97, RGBA32 1 |
| `_SpecularColorMap` | 200 | DXT5 171, DXT1 16, BC7 13 |
| `_BumpMap` | 116 | DXT5 116 |
| `_MetallicGlossMap` | 115 | DXT1 114, DXT5 1 |
| `_SpecGlossMap` | 114 | DXT1 114 |
| `_OcclusionMap` | 28 | DXT1 28 |
| `_EmissiveColorMap` | 35 | DXT1 35 |
| `_Reticle` | 40 | DXT5 33, RGBA32 7 |

Counts here are bindings that resolve inside the same bundle, which is why `_BaseColorMap` reads 2,056 rather than 2,058 and `_MainTex` 1,420 rather than 1,422.

### 2.6 The mask map, channel by channel

`_MaskMap` is HDRP's packed PBR texture. The convention is one channel per signal:

```
_MaskMap
├── R  Metallic
├── G  Ambient occlusion
├── B  Detail mask
└── A  Smoothness
```

Confidence on that packing is not a single answer, so take it in two parts.

**What the shader property block proves.** The `Shader` objects are co-shipped in the bundles and their property blocks are readable. `HDRP/Lit` (path id `7322088784485553867`, present in `charactermodmaterials_assets_anprc152_aor1_*`) declares 153 properties, 24 of them texture slots. Alongside the single `_MaskMap` slot it declares `_MetallicRemapMin`, `_MetallicRemapMax`, `_AORemapMin`, `_AORemapMax`, `_SmoothnessRemapMin`, `_SmoothnessRemapMax`, `_AlphaRemapMin` and `_AlphaRemapMax`. That proves, from shipped data, that one texture feeds metallic, AO and smoothness, and that each has its own remap range. It does not prove which channel is which. The property block carries names, types, flags and default texture names, not sampling code, and the HLSL is not in the bundle. The RGBA order is not confirmed from the property block.

**What pixel decoding proves.** Decoding the textures resolves it. Per-channel means over a 64x64 resample, with raw min and max:

```
MaskMap                (mi_mag_magpul_pmag_gen3_m3_556_30_blk, 2048 DXT5, linear)
  R=0.010 (0,153)   G=0.742 (0,255)   B=1.000 (255,255)   A=0.361 (16,255)
MI_PMAG_556_MaskMap    (mi_pmag_556, 2048 DXT1, linear)
  R=0.020 (0,216)   G=0.642 (0,255)   B=1.000 (255,255)   A=1.000 (255,255)
```

R near zero on a polymer magazine reads as metallic. G in the 0.6 to 0.75 band with full range reads as an AO bake. B is a hard constant. A at 0.361 on a matte polymer body reads as smoothness. That reading is consistent with HDRP's documented packing on every mask sampled, and the material carries the `_MASKMAP` keyword. Treat R metallic, G AO, A smoothness as confirmed. B detail is inferred from HDRP convention and from the fact that it is never authored: it decodes as a hard 255 or a hard 0, no material in the game sets `_UVDetail` to anything but 0.0 (2,543 of 2,543), and there are only 6 `_DetailMap` bindings against 1,952 declarations. B is free space.

**Two exceptions you must check before editing R.** First, there is a population where R is not metallic. `watch_low_hands_MaskMap` in `charactermodmaterials/hands` decodes R=0.968 (0,255), G=0.636 (0,255), B=0.000 (0,0), A=0.736 (101,216). R at 0.968 with full range cannot be metallic on a watch and a hand; it reads as AO in R, which is inference from the value distribution rather than a proven swap. Sample any skin, glove or hand mask before you touch R, because a mean above 0.9 means you are probably looking at an AO map wearing a MaskMap name, and editing it as metallic will chrome the model. Second, 116 materials bind an Unreal-ordered `*_OcclusionRoughnessMetallic` or `*_ORM` texture into `_MaskMap` while running a shader that is not `MilkShaders/UERemap`: 91 on `MilkShaders/Lit-Template`, 14 on `HDRP/Lit` (the `T_Glove*_ORM` set and five copies of `T_Arms_ORM 1_ConvertedMask`) and 11 with an unresolved shader. That population is *likely*, not proven; confirming it needs a runtime probe of the compiled `MilkShaders/Lit-Template` inside the game process, because a bundle read cannot tell you whether that shader remaps.

**The DXT1 mask trap.** 276 `_MaskMap` bindings, covering 168 distinct texture names, point at a DXT1 texture. DXT1 has no alpha, so smoothness decodes to a hard 1.0 everywhere: see `MI_PMAG_556_MaskMap` above, A min 255 max 255. Distribution of those 276 is 132 at 512x512, 65 at 2048x2048, 56 at 1024x1024, 14 at 4x4, 6 at 2048x1024 and 3 at 256x256. 224 of the 276 carry `*_OcclusionRoughnessMetallic` or `*_ORM` names where the fourth channel was never meant to exist. Some materials paper over the missing channel by clamping: 15 materials set `_SmoothnessRemapMax` to 0.0, forcing smoothness to zero regardless of the mask. If you upgrade one of those to a DXT5 mask you must also reset `_SmoothnessRemapMax` to 1.0, or your gloss data is discarded by the clamp and nothing changes on screen.

Slot defaults are otherwise uniform, which is useful as a baseline. Of the 2,543 materials carrying the remap floats, `_AORemapMin` is 0.0 on 2,475 and `_AORemapMax` is 1.0 on 2,498; `_SmoothnessRemapMin` is 0.0 on 2,421 and `_SmoothnessRemapMax` is 1.0 on 2,213; `_NormalScale` is 1.0 on 2,474. Of the 2,545 carrying `_Metallic`, it is 0.0 on 2,159 and 1.0 on 259.

### 2.7 Normal map convention

Normals are DXT5nm with the AG swizzle, tangent space, DirectX handedness. Confirmed by decoding, not inferred:

```
ANPRC152_Normal      R=1.000 (255,255)  G=0.489 (37,215)  B=0.490 (37,215)  A=0.496 (34,227)
PMAG_Black_Normal    R=1.000 (255,255)  G=0.500 (11,248)  B=0.501 (11,251)  A=0.494 ( 9,238)
Mag_Magpul_PMAG_GEN3_M3_556_30_Camo_DirectX_Normal
                     R=1.000 (255,255)  G=0.503 (28,199)  B=0.503 (28,199)  A=0.486 (36,198)
```

R is a hard constant 255. G and B carry the same signal; where their min and max differ slightly, as on `PMAG_Black_Normal`, that is DXT5's RGB565 endpoint quantisation giving green one more bit than blue, not two different signals. A carries a second, independent signal centred on 0.5. That is Unity's normal-map import output on DirectX: X in alpha, Y in green, R forced to 1 as the detection flag, B wasted. HDRP's `UnpackNormalmapRGorAG` multiplies x by w and keys off R=1 to take X from alpha; that behaviour comes from the HDRP shader library, not from anything in these bundles, since the HLSL is not shipped here. Materials binding a normal carry the `_NORMALMAP` and `_NORMALMAP_TANGENT_SPACE` keywords, confirming tangent space rather than object space. `charactermodmaterials/anprc152_aor1` material `ANPRC152_AOR1` carries `_DISABLE_SSR_TRANSPARENT`, `_MASKMAP`, `_NORMALMAP`, `_NORMALMAP_TANGENT_SPACE`, and `charactermodmaterials/mi_pmag_556` material `MI_PMAG_556` carries the same four.

If you author a plain RGB tangent normal and inject it as DXT5 without Unity's normal-map import, R becomes your X channel instead of 255, `UnpackNormalmapRGorAG` multiplies X by your alpha, and the surface lights wrong, usually flat or inverted on one axis. Replacements must be re-encoded to DXT5nm, either by importing as Normal Map in a Unity project and re-exporting, or by hand-packing `(255, Y, Y, X)`. Only 4 DXT1 and 1 RGBA32 texture are bound to `_NormalMap` in the entire game, so a non-DXT5 normal is almost always a mistake, not a style.

### 2.8 The dual `_BaseColorMap` and `_MainTex` binding

1,296 materials bind both `_BaseColorMap` and `_MainTex`, and in all 1,296 cases the two properties point at the same texture path id. Zero disagreements. 1,268 of those are `HDRP/Lit` and 28 have a shader pointer that does not resolve inside the bundle. A further 762 materials bind only `_BaseColorMap`: `MilkShaders/Lit-Template` 438, `MilkShaders/UERemap` 206, `ClothWind` 101, `HDRP/Hair` 4, `HDRP/Decal` 4 and 7 unresolved. 126 bind only `_MainTex`, of which 115 are `HDRP/Autodesk Interactive/AutodeskInteractive`, 8 are KriptoFX particle and distortion shaders and 2 are TextMeshPro.

The shader property block explains the mirror. `HDRP/Lit` declares:

```
_BaseColor      type Color    flags 256   (MainColor)
_BaseColorMap   type Texture  flags 128   (MainTexture)   default "white"
_MainTex        type Texture  flags   1   (HideInInspector), description "Albedo", default "white"
_NormalMap      type Texture  flags   0                   default "bump"
_MaskMap        type Texture  flags   0                   default "white"
_HeightMap      type Texture  flags   0                   default "black"
_DetailMap      type Texture  flags   0                   default "linearGrey"
```

The flag values are Unity's `ShaderPropertyFlags` bits, so 256 is MainColor, 128 is MainTexture and 1 is HideInInspector. `_BaseColorMap` carries the MainTexture flag and `_MainTex` is a hidden legacy alias kept so that code doing `renderer.material.mainTexture` still finds something. Practical consequence: if you rebuild an `HDRP/Lit` material and set only one of the two, half the pipeline reads the old texture. Set both, to the same path id.

### 2.9 Resolution split by family

| Resolution | charactermodmaterials | charactermods_stripped | weaponmodmaterials | weaponmods_stripped | other | Total |
|---|---:|---:|---:|---:|---:|---:|
| 4096x4096 | 11 | 0 | 3 | 3 | 0 | 17 |
| 2048x2048 | 1,797 | 44 | 2,091 | 34 | 5 | 3,971 |
| 2048x1024 | 0 | 0 | 115 | 3 | 0 | 118 |
| 1024x1024 | 802 | 222 | 904 | 35 | 0 | 1,963 |
| 1024x512 | 0 | 0 | 33 | 0 | 0 | 33 |
| 512x512 | 447 | 0 | 403 | 16 | 0 | 866 |
| 256x512 | 0 | 0 | 3 | 12 | 0 | 15 |
| 256x256 | 26 | 0 | 41 | 15 | 0 | 82 |
| 128x128 | 11 | 4 | 0 | 43 | 0 | 58 |
| 64x64 | 0 | 0 | 6 | 12 | 0 | 18 |
| 32x32 | 8 | 39 | 5 | 11 | 0 | 63 |
| other | 1 | 5 | 37 | 0 | 0 | 43 |
| **Total** | **3,103** | **314** | **3,641** | **184** | **5** | **7,247** |
| Payload | 8.26 GiB | 0.36 GiB | 9.87 GiB | 0.23 GiB | 0.02 GiB | 18.75 GiB |

The two `*materials` families hold 93.1% of the textures and 96.7% of the payload, and both centre on 2048 square. The `*_stripped` prefab families are not texture-free, contrary to what the bundle naming suggests: 498 textures live in them. Their distributions differ in kind. `charactermods_stripped` skews to 1024 (222 of 314), which is where the camo override swatches and their masks sit, and its 39 textures at 32x32 are the `T_Pouches_*` camo and colour swatch set, 17 diffuse variants plus 22 duplicated copies of a 32x32 `T_Pouches_N` normal. `weaponmods_stripped` is split about evenly between art and non-art, with 43 of its 184 textures being the 128x128 IES light cookies. The `other` column is one bundle, `defaultlocalgroup_assets_mat_carryhandle_2034f6e90a7a07d4d2906186afbee036.bundle`, holding `M4 Carryhandle_COL`, `_RGS`, `_MET`, `_AO` at 2048 DXT1 and `M4 Carryhandle_NRM` at 2048 DXT5.

### 2.10 What a replacement has to match

Author to the neighbours, not to your own preference. A replacement that differs in any of the five properties below will either look wrong next to unmodified siblings or fail to load.

| Property | Rule | Why |
|---|---|---|
| Dimensions | Match the original exactly, not "at least as big" | A different base size changes the payload length, which shifts every subsequent `m_StreamData.offset` in the `.resS` and breaks the textures after yours |
| Format family | DXT1 for base colour, DXT5 for normals and masks | That covers 96.11% of the library and it is what the slot expects: 1,914 of 2,056 `_BaseColorMap` bindings are DXT1, 1,979 of 2,076 `_NormalMap` bindings are DXT5 |
| Mip chain | Full chain or exactly one level, matching the original | 98.23% carry a complete chain and the rest carry exactly one; a changed mip count changes the payload size and the loader reads past the end of the blob |
| Streaming layout | If the original had `m_StreamData` populated, yours must too, with a correct offset and size and every later block fixed up | 56 textures are inline, mostly IES cookies; keep those inline |
| Colour space flag | Match the original's `m_ColorSpace` even where it is wrong | See below |

Do not reach for BC5 on normals, it appears nowhere in the game. Do not reach for BC7 unless you are replacing one of the 216 that already is. If the mip count changes at all, rebuild through a Unity project rather than patching bytes.

The colour space flag needs its own note. A full re-probe of all 7,247 textures gives 2,959 flagged sRGB and 4,288 linear. Classifying by name role, somewhere between 412 and 491 textures carrying non-colour data are flagged sRGB; the spread is the sensitivity of the name classifier, not the data. The individually verified cases are firm: all 10 copies of `CRYE_G3_MC_Opacity` across the Crye G3 camo family are sRGB, 25 of the 205 `LitMask` copies are sRGB while the other 180 are linear, and 5 normal maps are sRGB (`aviators_low_glasses_Normal`, `Knife_SWK_Guardian_DirectX_Normal`, `Patch_US_IR_Normal`, `Patch_Snake_Normal`, `PatchesGenerator_Normal 1`). Setting the flag correctly on one texture makes it render brighter than its unedited siblings. Either match the wrong flag, or fix the whole family in one pass.

Two rules on top of the checklist. Never search and replace by texture name: 1,179 of the 3,927 distinct names appear in more than one bundle, `MaskMap` appears in 274 bundles at three different resolutions and `LitMask` in 205 at five, and those are different images that happen to share a string. And when you edit a mask, sample it first, because the slot name does not tell you the packing.

## 3. Shaders and their properties

Twenty-three named shaders paint 2,399 of the 2,603 Materials in the 4,989 live bundles. The other 204 carry a shader reference into another bundle, and once you follow it, six shaders account for 2,469 of the 2,603, about 95 percent of the art. The registry behind them is much larger: 351 Shader objects in `globalgamemanagers.assets`, of which 214 are `Hidden/*` engine and VFX internals and 137 are author-facing. Only 23 of those 137 ever reach a bundle. Knowing which shader a material binds decides which property names do anything, and getting that wrong is the most common reason a correct-looking reskin changes nothing on screen.

### 3.1 The census

Computed from `materials.tsv`, all 2,603 rows, no sampling. The family column splits the bundle name on `_assets_`.

| Shader | Materials | Bundle families |
|---|---:|---|
| `HDRP/Lit` | 1314 | 729 weaponmodmaterials, 560 charactermodmaterials, 25 charactermods_stripped |
| `MilkShaders/Lit-Template` | 439 | 286 charactermodmaterials, 153 weaponmodmaterials |
| `MilkShaders/UERemap` | 207 | 207 weaponmodmaterials |
| *(resolves externally)* | 204 | 128 charactermods_stripped, 76 weaponmods_stripped |
| `HDRP/Autodesk Interactive/AutodeskInteractive` | 117 | 75 charactermodmaterials, 39 weaponmodmaterials, 2 weaponmods_stripped, 1 defaultlocalgroup |
| `Shader Graphs/ClothWind` | 109 | 109 charactermodmaterials |
| `HDRP/Unlit` | 80 | 78 weaponmods_stripped, 2 weaponmodmaterials |
| `Ultimate Scope Shaders/HolographicSight` | 39 | 39 weaponmodmaterials |
| `Shader Graphs/RealisticNVG 2` | 39 | 38 weaponmods_stripped, 1 weaponmodmaterials |
| `Shader Graphs/RealisticScopeEffect` | 18 | 18 weaponmodmaterials |
| `Shader Graphs/Reticle` | 8 | 4 weaponmodmaterials, 4 weaponmods_stripped |
| `KriptoFX/FPS_Pack/Particles` | 6 | 6 weaponmodmaterials |
| `Shader Graphs/_NonDualRender RealisticScopeEffect` | 5 | 4 weaponmods_stripped, 1 weaponmodmaterials |
| `HDRP/Hair` | 4 | 4 charactermodmaterials (`Beard1`, `Beard2`, `Beard3`, `MI_Eyelashes`) |
| `HDRP/Decal` | 4 | 4 weaponmods_stripped (`Decal` x2, `Decal 1` x2) |
| `GoreShader` | 2 | 1 charactermodmaterials (`M_Head`), 1 charactermods_stripped (`M_PouchesCamoC`) |
| singletons | 8 | `Shader Graphs/IR Laser`, `Ultimate Scope Shaders/Scope`, `KriptoFX/FPS_Pack/{Distortion, GlowAdditiveNoFade, AlphaBlendedAnim}`, two TextMeshPro variants, `Sprites/Default` |

The split by family is the useful read. Character garments and soft gear are `HDRP/Lit`, `MilkShaders/Lit-Template` and `ClothWind`; weapons are `HDRP/Lit`, `Lit-Template` and `UERemap`; `Autodesk Interactive` is the FBX-imported branch and spans both, 75 character-side bags, backpacks, boots and pouches plus one `MI_MPU5` radio, and 41 weapon-side barrels, handguards, stocks and suppressors. Optics own the rest.

`HDRP/Unlit` is not gear. Seventy-eight of its 80 materials are the optic eye-relief helper, named `Eye Relief Visualization` or `Eye Relief Visualization 1` inside the prefab bundles; the other two are `scope_pso6x36 1` and `Vis Laser`. All 80 bind zero textures, although they serialize four empty texture slots (`_UnlitColorMap`, `_MainTex`, `_EmissiveColorMap`, `_DistortionVectorMap`).

`Shader Graphs/PaintColorShift`, the obvious hook for a weapon paint system, is in the registry and is used by exactly zero bundle materials.

### 3.2 "Stripped" is the wrong word, and the 204 external materials prove it

Every *materials* bundle ships its own compiled shader, all 2,228 of them, zero exceptions. Opening `charactermodmaterials_assets_mi_crye_ls_mcworn_c40fa95cf38eb678d1b8be3d800bed0b.bundle` gives seven objects: one Material, three Texture2D, two Shaders (`Shader Graphs/ClothWind` and `Hidden/Shader Graph/FallbackError`), and the AssetBundle manifest. The Material's `m_Shader` PPtr is `{m_FileID: 0, m_PathID: 6692683703723378954}`, file ID zero meaning the shader inside this same file. Same story for `weaponmodmaterials_assets_525pm2blk_cc2863de3bbef9d5d099cd66740863b4.bundle`, whose `525pm2 BLK` points at `{m_FileID: 0, m_PathID: -8225816520607640240}`, `MilkShaders/UERemap`, packed alongside. Path IDs are stable: across all 2,603 materials, 23 distinct shader path IDs map to 23 distinct shader names with no collision, so `7322088784485553867` is `HDRP/Lit` everywhere it appears and you can fingerprint a material's shader without resolving a name.

Prefab bundles are the opposite. Only 73 of the 2,746 `*_stripped` bundles carry a Shader object at all, which is why their materials look broken.

The 204 materials whose shader name does not resolve are all in those two prefab families, 106 named `DefaultHDMaterial`, 39 named `Scope Lens`, 19 named `MI_MISC_02`, then a long tail. Their PPtr is not broken, it is external. `charactermods_stripped_assets_avsmcbslick_42557949de20ca4e11ad2c6f3c1fdf48.bundle` carries `DefaultHDMaterial` with `m_Shader = {m_FileID: 2, m_PathID: 7322088784485553867}`, and external slot 2 in that file's reference table is `archive:/CAB-5b00af9b95ebb9aec2086aa927439000`, which is `weaponmodmaterials_assets_modplaceholdermaterial_26ab66620194ad9caab63da2f7a35e00.bundle`. That bundle holds three Shaders, one Material and the AssetBundle manifest, nothing else:

```
weaponmodmaterials_assets_modplaceholdermaterial_26ab66620194ad9caab63da2f7a35e00.bundle  (1,902,551 B)
├─ Shader   HDRP/Lit                     pathID  7322088784485553867
├─ Shader   Hidden/Core/FallbackError    pathID -8015457887526078469
├─ Shader   Hidden/HDRP/FallbackError    pathID  1354542265007958578
├─ Material "mod placeholder material"   pathID -5343436609273763532  -> m_FileID 0
└─ AssetBundle (manifest)
```

The placeholder bundle is not the only donor. Resolving the external slot for all 204 materials, in every one of the 178 prefab bundles that carries one, gives eleven destinations:

| Donor bundle (by slug) | Materials served | Shader borrowed |
|---|---:|---|
| `modplaceholdermaterial` | 119 | `HDRP/Lit` |
| `charactermodmaterials .. jpc_v2` | 27 | `HDRP/Lit` |
| `charactermodmaterials .. mi_patch_3dma_us_ir` | 27 | `HDRP/Lit` |
| `charactermodmaterials .. mi_mpu5_mc` | 18 | `HDRP/Lit` |
| `weaponmodmaterials .. ak_mount` | 3 | `MilkShaders/Lit-Template` |
| `weaponmodmaterials .. acog` | 3 | `MilkShaders/UERemap` |
| `weaponmodmaterials .. acog_rearglass` | 3 | `HDRP/Lit` |
| `weaponmodmaterials .. scope` | 1 | `HDRP/Lit` |
| `weaponmodmaterials .. specterlesn` | 1 | `HDRP/Lit` |
| `weaponmodmaterials .. acog_mount` | 1 | `MilkShaders/UERemap` |
| `weaponmodmaterials .. pvs30` | 1 | `Shader Graphs/RealisticNVG 2` |

By shader that is 196 on `HDRP/Lit`, four on `UERemap`, three on `Lit-Template`, one on `RealisticNVG 2`. The practical consequence is that a prefab bundle borrows its compiled shader from whichever material bundle Addressables happened to list as a dependency, so loading a prefab without its dependency set leaves that material unbound. Prefab bundles are also mixed rather than uniform: `weaponmods_stripped_assets_burrisxtr141-4x24_1c50908ed661b6275f1dc04f08813df3.bundle` carries four materials, three with in-bundle shaders (`HDRP/Unlit` twice, `RealisticNVG 2` once) and one, `Scope Lens`, resolving externally to the placeholder bundle.

Two consequences. First, a material bundle is self-contained; replace the Texture2D payload and the shader binding survives, so no `Shader.Find` rebinding is needed. `Shader.Find` still matters for materials you create fresh at runtime and for any prefab you instantiate without its dependency bundles loaded. Second, every packed Shader ships with `platforms = [4]`, D3D11 only, so a shader you add yourself must have a D3D11 variant or it drops to `FallbackError`. The build carries 5,900 Shader objects across 2,303 bundles, which is the same handful of shaders repacked once per bundle.

There is a naming trap that has produced contradictory notes elsewhere. Every packed Shader has `m_Name == ''`. The real name lives in `m_ParsedForm.m_Name`. If you read `mat.m_Shader.read().m_Name` you get an empty string and conclude the shaders are stripped; read `m_ParsedForm.m_Name` and 2,399 of 2,603 materials resolve without leaving the bundle.

### 3.3 MilkShaders/UERemap, and what it actually binds

The name implies an Unreal channel remap and the data supports it. `UERemap` declares 76 properties. The first 18 are author-facing; the remaining 58 are the HDRP pipeline block plus the three `unity_*` lightmap arrays. Exactly four are texture slots you can use:

```
MilkShaders/UERemap  (pathID -8225816520607640240, 76 props, fallback Hidden/Shader Graph/FallbackError)
  _BaseColorMap  _BaseColor
  _NormalMap     _NormalScale   _USE_UNREAL_NORMAL_MAP
  _MaskMap       _AORemapMin/_AORemapMax          [inspector label: "ORM"]
                 _NEGATE_ROUGHNESS
                 _SmoothnessRemapMin/_SmoothnessRemapMax
                 _MetallicRemapMin/_MetallicRemapMax
  _Emission      _Emmisive_Color                  [sic, doubled m]
  _Tiling  _Offset  _Alpha_Clip_Threshold
```

Across all 207 materials, `_MaskMap` is bound on 207, `_BaseColorMap` on 206, `_NormalMap` on 206, `_Emission` on 8. Every one of the 207 `_MaskMap` bindings is an Unreal ORM texture by name: 197 `*_OcclusionRoughnessMetallic`, seven `*_orm`, three `*_ORM`. Not one is an HDRP-packed MaskMap. The shader's own `m_PropInfo` labels `_MaskMap` as "ORM" in the inspector, so the intent is documented in the build rather than inferred. The remap arithmetic itself is not decompiled; reading R as ambient occlusion, G as roughness and B as metallic and rewriting into HDRP's R metallic, G AO, B detail, A smoothness is the interpretation the naming and the label support, not a decompiled fact.

Two hundred and six of the 207 masks are DXT1, which has no alpha, and that is consistent with ORM needing only three channels. Bind the same DXT1 texture on `HDRP/Lit` and alpha reads 1.0, so smoothness pins to `_SmoothnessRemapMax`.

The two toggles are set consistently in both places they need to be. `_USE_UNREAL_NORMAL_MAP` is 1.0 on 187 of 207 materials and the matching keyword is in `m_ValidKeywords` on the same 187. `_NEGATE_ROUGHNESS` is 1.0 on 15 and keyworded on the same 15: the four MDT chassis materials, both Mossberg 590 forearms, five Noveske materials, both `virtus_grip` materials, `stanag_mag` and `rmr glass`. The 20 materials with the normal-map toggle off include `M17` and `M17 Black`, whose normal is named `M17_N_OpenGL`, and both Noveske accessory materials, whose normal is `T_noveske_accessories_n`. The naming implies the toggle is a green-channel convention flip that is off when the source was already exported green-up. The remaining 16 bind plainly named `*_Normal` maps (acog, acog mounts, ftc mounts, `magpul_ctr` and `magpul_ctr_075_riser`) and that is unexplained; it needs a pixel probe.

Worth acting on: 98 material-to-`_MaskMap` bindings across 65 distinct textures feed a raw Unreal ORM into a shader that is not `UERemap`, 80 on `Lit-Template`, 11 on the external placeholder materials, and seven on `HDRP/Lit` (`T_Glove2_ORM`, `T_Glove3_ORM`, `T_Glove5_ORM`, `T_Glove6_ORM`, `T_Glove7_ORM`, plus `vudu16_OcclusionRoughnessMetallic` on two materials). Those surfaces read metallic from AO, AO from roughness, and smoothness from a channel that does not exist. The fix pattern already ships: eight repacked textures are in the build, seven named `*_ConvertedMask` and one `tango6t_OcclusionRoughnessMetallic_converted`, feeding 18 bindings. Repacking the other 65 into HDRP order is the cheapest correctness win in the texture set, and it needs no shader or geometry change.

### 3.4 Shader Graphs/ClothWind, and what drives cloth motion

All 109 `ClothWind` materials are character garments in `charactermodmaterials`: the Crye G3 combat shirt and pants families, the Patagonia jacket and flannel, Levi's jeans, PCU, rugby shirts, T-shirts, and the NPC `M_*` set. The shipped typo `Greem` is one of them, and so is a material named `stupid`.

Bindings across the 109: `_MaskMap` 109, `_NormalMap` 109, `_BaseColorMap` 101, `_OpacityMap` 10, `_Emission` 0, `_Heli_Wind_Mask` 0. The eight with no albedo are the tint-only colours, `Blue`, `Brown`, `FDE`, `Gray`, `Greem`, `red`, `RG` and `MI_TShirt_3DMA_Snake`, each carrying its colour in `_BaseColor` as a float4 (`Greem` is `0.1686, 0.3098, 0.1529, 1.0`). The 10 with an opacity map all bind the same `CRYE_G3_MC_Opacity`, 2048x2048 DXT1 with 12 mips, and all 10 carry both `_USE_OPACITY_MAP = 1` and the `_ALPHATEST_ON` keyword, and sit at render queue 2475 where the other 99 sit at 2225.

The wind inputs are two properties and nothing else. `_Wind_Strength_Multiplier` is 1.0 on 100 materials and 0.5 on nine. `_Heli_Wind_Mask` is declared with a default texture of `white` and is unbound on all 109, so an unbound sampler reads 1.0 and the displacement applies at full strength across the whole garment. If you want wind confined to a hem or a flap you must author a mask and bind it; there is no shipped example to copy.

`ClothWind` uses remap pairs, not single values, and this is where garment mods usually fail. The shader declares `_Metallic_Min`/`_Metallic_Max`, `_AO_Min`/`_AO_Max`, `_Smoothness_Min`/`_Smoothness_Max`. It does not declare `_Metallic`, `_Smoothness` or `_AORemapMin`, yet all 109 materials serialize `_Smoothness` (0.5 on 75, 0.483 on 18, 0.447 on 16), all 109 serialize `_Metallic` and `_AORemapMin`, and 108 serialize `_WindStrength`. Those are leftovers from a previous shader assignment. `SetFloat("_Smoothness", x)` on a `ClothWind` garment does nothing; you want `_Smoothness_Min` and `_Smoothness_Max`, which sit at the identity 0.0 on 108 and 1.0 on 107 respectively.

### 3.5 Optics, NVG and particles

`Ultimate Scope Shaders/HolographicSight` covers 39 red-dot and holographic materials. Its only texture slot in use is `_Reticle`, bound on all 39, most often to `Reticle03` (1000x1000 DXT5, one mip, duplicated into 15 bundles and bound by 15 of the 39). Everything else is numeric: `_Reticle_Color` as an HDR colour (`2.9961, 0, 0, 1` is the single most common value, and one material sits at `8096.46` on all three channels), `_Reticle_Brightness` from 0.5 to 2560.0, `_Retical_Size` (shipped typo, spell it wrong), `_Glass_Tint`, `_Depth`, and `_NVG_Reticle` which is 1.0 on 17 of the 39, marking the NVG twin of each optic. Feature branches are gated twice, `_BLURRETICLE` is 1.0 on 27 and `_USERADIALNOISE` on 35, and each is both a float and a shader keyword. Set only the float and the branch stays compiled out.

`Shader Graphs/Reticle` (8 materials) is the incompatible sibling and uses property names with no leading underscore at all: `Reticle`, `Reticle_Brightness`, `Reticle_Size`, `Reticle_Rotation`, `Reticle_X_Offset`, `Reticle_Y_Offset`, `Depth`, `Override_Colour`, plus `_Color` for the tint. `Shader Graphs/RealisticScopeEffect` (18) and `_NonDualRender RealisticScopeEffect` (5) follow the same unprefixed convention. `SetFloat("_Reticle_Brightness", x)` on a `Shader Graphs/Reticle` material is a no-op, and so is `SetFloat("Reticle_Brightness", x)` on a `HolographicSight` one.

`Shader Graphs/RealisticNVG 2` has 39 materials, 38 of them named `Scope NVG Filter` living inside `weaponmods_stripped` prefab bundles rather than material bundles, plus `PVS30`. It declares 80 properties, 34 of them author-facing, covering the gain model (`_MaxGain`, `_Target_Level`, `_min_level`, `_gain_response`, `_FOM`, `_Autogating`, `_Additional_Brightness`), the eyebox (`EyeBox_Size`, `EyeBox_Softness`, `Lens_Eye_Relief_Size`, `_EyeBoxOffset`, `_EyeBoxAspect`), the vignette, and a noise block (`_Noise_Texture`, `_Speed`, `_Noise_Density`, `_Noise_Opacity`, `_Noise_Edge`, `_Noise_Intensity`). Note the mixed convention inside one shader: the gain and noise properties are underscore-prefixed, the eyebox ones are not. Two properties were never renamed out of Shader Graph, `Vector1_CD70A700` and `Vector1_B89D9867`, so you need those exact strings.

The KriptoFX branch is nine materials total: six on `KriptoFX/FPS_Pack/Particles` (`Flame1`, `Flame2`, `MuzzleFlash1`, `MuzzleFlash2`, `MuzzleFlash4`, `MuzzleFlash5`), plus `MuzzleParticles` on `GlowAdditiveNoFade`, `Smoke` on `AlphaBlendedAnim` and `Distortion` on `Distortion`. `KriptoFX/FPS_Pack/Particles` declares exactly two properties, `_TintColor` and `_MainTex`, which makes muzzle-flash recolouring a one-line change and muzzle-flash reshaping a texture swap.

### 3.6 Shader names that resolve and still render wrong

`Shader.Find` failure comes in two flavours here, and they fail differently. Nothing beginning with `Mobile/` exists in the 351, so `Shader.Find("Mobile/Particles/Additive")` returns null, `material.shader` becomes null, and Unity substitutes its internal error shader. Five built-in render pipeline shaders do ship and do resolve, and under HDRP they draw magenta: `Legacy Shaders/Particles/Additive`, `Legacy Shaders/Particles/Alpha Blended`, `Legacy Shaders/Particles/Alpha Blended Premultiply`, `Legacy Shaders/VertexLit`, and `Particles/Standard Unlit`, whose declared `m_FallbackName` is `VertexLit`, so the chain never reaches anything HDRP can draw. `Skybox/Cubemap` and `Skybox/Procedural` are the same class of built-in leftover; HDRP renders sky through `HDRP/PhysicallyBasedSky`, so those two are unreachable in practice, though that has not been confirmed by a scene sweep.

When a material does fall through, the fallback it lands on tells you which family it came from: `Hidden/Shader Graph/FallbackError` for Shader Graph materials, `Hidden/HDRP/FallbackError` for hand-written HDRP, `Hidden/Core/FallbackError` for SRP core. All three ship inside the art bundles, which is why a broken material still draws magenta rather than disappearing. For custom particles use `Vefects/SH_Vefects_HDRP_VFX_Opaque_Particle_01`, `Knife/HDRP Particle`, `Shader Graphs/ParticleLit` or the KriptoFX set, never `Legacy Shaders/Particles/*`.

### 3.7 The serialized property block is a graveyard

Across `props.tsv` and `texenvs.tsv` there are 441,712 serialized property rows. Only 64.2 percent name a property the bound shader actually declares. Another 26.9 percent (118,692 rows) name a property the shader does not declare at all, and 8.9 percent (39,491 rows) belong to the 204 materials whose shader is external. Unity keeps every property a material has ever been assigned, so `m_SavedProperties` accumulates residue from earlier shader assignments and never prunes it.

| Shader | Declares | Distinct names its materials serialize | Not declared |
|---|---:|---:|---:|
| `HDRP/Lit` | 153 | 886 | 733 |
| `MilkShaders/Lit-Template` | 82 | 600 | 519 |
| `MilkShaders/UERemap` | 76 | 184 | 108 |
| `Shader Graphs/ClothWind` | 76 | 180 | 104 |
| `HDRP/Autodesk Interactive/AutodeskInteractive` | 76 | 186 | 110 |
| `HDRP/Unlit` | 62 | 180 | 118 |

`Lit-Template` is the worst case: 412 of its 592 serialized float and colour names appear on three or fewer materials, and they include Bakery lightmapping flags (`_BAKERY_BICUBIC`, `_BAKERY_VOLUME`), cheap subsurface (`_CheapSSSPower`), and six-sided lighting (`_6SidedAngleContrast`), each on exactly one material. None of it does anything. Never infer a shader's interface from a material dump; read the shader's `m_PropInfo`.

Keywords are the enforced half of the same idea, and Unity records the split for you. `m_ValidKeywords` holds keywords the bound shader accepts, `m_InvalidKeywords` holds ones it does not. Read across all 316 `UERemap` and `ClothWind` bundles:

```
MilkShaders/UERemap  (207 materials)
  valid   _DISABLE_SSR_TRANSPARENT 207 | _USE_UNREAL_NORMAL_MAP 187 | _NEGATE_ROUGHNESS 15
          _DISABLE_SSR 6 | _ALPHATEST_ON 5 | _ENABLE_FOG_ON_TRANSPARENT 5
          _SURFACE_TYPE_TRANSPARENT 5 | _DISABLE_DECALS 4 | _DOUBLESIDED_ON 1
  invalid _NORMALMAP_TANGENT_SPACE 207 | _NORMALMAP 108 | _MASKMAP 33 | _SPECULARCOLORMAP 11
  renderQueue  2225 x194, 3000 x5, 2475 x4, 2000 x4

Shader Graphs/ClothWind  (109 materials)
  valid   _DISABLE_SSR_TRANSPARENT 109 | _ALPHATEST_ON 10 | _USE_OPACITY_MAP 10
  invalid _NORMALMAP_TANGENT_SPACE 109 | _NORMALMAP 55 | _MASKMAP 4
  renderQueue  2225 x99, 2475 x10
```

`HDRP/Lit` branches on `_MASKMAP`, `_NORMALMAP`, `_NORMALMAP_TANGENT_SPACE`, `_DETAIL_MAP` and `_EMISSIVE_COLOR_MAP` at compile time, so on an `HDRP/Lit` material `SetTexture("_MaskMap", t)` binds the texture and changes nothing unless `_MASKMAP` is in `m_ValidKeywords`. The Shader Graph shaders (`MilkShaders/*`, `ClothWind`) sample unconditionally, which is why their materials only carry `_DISABLE_SSR_TRANSPARENT` and push the HDRP keywords into `m_InvalidKeywords`. That is a real argument for retargeting a reskin onto `Lit-Template` rather than `HDRP/Lit`.

### 3.8 What you can drive, and what is baked

You can drive, at runtime or by editing `m_SavedProperties` in the bundle, any property the bound shader declares. Textures, colours, floats and vectors are all live. The three that matter most per family are the base colour map, the mask, and the normal, and their slot names differ by family, which is the single most common reskin mistake:

| Shader | Albedo slot | Mask or gloss slots | Normal slot |
|---|---|---|---|
| `HDRP/Lit` | `_BaseColorMap` **and** `_MainTex` | `_MaskMap` | `_NormalMap` |
| `MilkShaders/Lit-Template` | `_BaseColorMap` | `_MaskMap` | `_NormalMap` |
| `MilkShaders/UERemap` | `_BaseColorMap` | `_MaskMap` (ORM order) | `_NormalMap` |
| `Shader Graphs/ClothWind` | `_BaseColorMap` | `_MaskMap` | `_NormalMap` |
| `HDRP/Autodesk Interactive` | `_MainTex` | `_MetallicGlossMap`, `_SpecGlossMap`, `_OcclusionMap` | `_BumpMap` |

`HDRP/Lit` mirrors the albedo into both slots, and this is exact rather than approximate: of the 1,268 `HDRP/Lit` materials that bind an albedo, all 1,268 have the identical texture path ID in `_BaseColorMap` and `_MainTex`, zero exceptions, and no material binds one slot without the other. Set both or half the pipeline reads the old texture. The 117 Autodesk Interactive materials do not declare `_BaseColorMap` at all, so writing it is a no-op; their maps arrive as separate `*_Roughness` (113 bindings on `_SpecGlossMap`), `*_Metalness` (75 on `_MetallicGlossMap`) and `*_AO` (20 on `_OcclusionMap`) files.

Baked, and not reachable by any property write: the pixels inside a texture, including ORM versus MaskMap channel order and the mip chain; UV layout and mesh; the shader graph's own logic, so cloth displacement shape, NVG grain algorithm and scope optical model are code, not data; the compiled variant set, which is D3D11 only; and the keyword-gated branches, which need `EnableKeyword` alongside the float, not instead of it.

Two items remain open. First, the serialized data says a bundle-carried material keeps its in-bundle shader binding (`m_FileID = 0`), but this has not been observed live, and `ShaderWarmup`'s `allShaderVariantsCollection` is fixed at splash, so a variant not present at boot may still miss. The probe is one line: load a `charactermodmaterials` bundle after `ShaderWarmup.IsComplete` and log `mat.shader.name` together with `mat.shader.isSupported`. Until that runs, treat "the shader binding survives a bundle edit" as strongly indicated by the serialized form rather than confirmed in the running game. Second, the 16 `UERemap` materials that turn `_USE_UNREAL_NORMAL_MAP` off while binding conventionally named `*_Normal` maps have no explanation from the serialized data; deciding whether their green channel is flipped needs a pixel comparison against a sibling that leaves the toggle on.

## 4. Camouflage and colourways

Three separate mechanisms produce a camo option in this game, and they do not share a code path, a file layout, or a naming convention. Pick the wrong one and your edit binds correctly and changes nothing on screen. This section proves each mechanism against the shipped bundles, then gives the Crye G3 trees and the authoring procedure.

Sizes below are read from the live `StandaloneWindows64` directory. Where a size is quoted in MiB it is bytes divided by 1048576, not decimal MB.

### 4.1 Mechanism one: a colourway is a whole Material in its own bundle

A colourway is not a material index, not a submesh, and not a texture swap on a shared material. It is a separate `Material` asset in its own Addressables bundle, whose only meaningful difference from its siblings is the `_BaseColorMap` texture. Of the 1,025 `charactermodmaterials` bundles, 1,022 hold exactly one `Material`; only `jpc_v2`, `delta` and `gatorz_folded` hold more. `MI_CRYE_Pants_M81` and `MI_CRYE_Pants_MC` are two files:

```
charactermodmaterials_assets_mi_crye_pants_m81_cb3ba44b14c5ac99e5a250a06c058387.bundle   3,824,343 B
  Material  MI_CRYE_Pants_M81      shader Shader Graphs/ClothWind (pathID 6692683703723378954)
    _BaseColorMap  "crye retexture_MI_CRYE_Pants_MC_BaseMap"  2048x2048 DXT1 m12   <- the only real difference
    _MaskMap       "CryePantMask"                             1024x1024 DXT5 m11
    _NormalMap     "CRYE_G3_MC_Normal_12"                     1024x1024 DXT5 m11
    _BaseColor     (1, 1, 1, 1)   _AlphaCutoffEnable 0   _BlendMode 0   _SurfaceType 0 (opaque)
    m_ValidKeywords   ['_DISABLE_SSR_TRANSPARENT']
    m_InvalidKeywords ['_NORMALMAP_TANGENT_SPACE']
    m_Container       Assets/1Assets/1CGTrader/3DMA/Uniform_CRYE/Textures/PBR CRYE M81/MI_CRYE_Pants_M81.mat

charactermodmaterials_assets_mi_crye_pants_mc_c5fc13f8998bfe81afbd13fc7cd3f40e.bundle    3,812,234 B
  Material  MI_CRYE_Pants_MC       identical in every respect except
    _BaseColorMap  "CRYE_G3_MC_BaseColor_9"                   2048x2048 DXT1 m12
```

Texture envs are effectively always identity. Across all 44,144 `m_TexEnvs` rows in the game, 7 carry a scale other than `(1,1)`, every one of them on `_DetailMap`, and only one of those (`M_Arms`) sits in `charactermodmaterials`. Offsets are `(0,0)` everywhere.

Do not assume `_BaseColor` is white. It is `(1,1,1,1)` on all 50 Crye G3 materials, but of the 925 `charactermodmaterials` materials that bind a `_BaseColorMap`, 771 are white and 154 carry a grey multiplier. Five of those sit on garment shaders you are likely to donor from: `MI_Jacket_Patagonia 1` (0.9063), `MI_Rugby_Shirt_FDE 4` (0.9340), `MI_PCU_Grey` (0.9063), `MI_Jeans_LEVIS 1` (0.2358) and `MI_Jeans_LEVIS Gray` (0.5094). Read the float before you paint against it.

Note the shader. Crye, Patagonia, Rugby, PCU, Jeans and the T-shirts are **`Shader Graphs/ClothWind`**, 109 materials in total, even though they use HDRP property names. Helmets, helmet covers, Boonie hats and hard gear are `HDRP/Lit` (585 materials on the character side). Plate carriers and pouches are `MilkShaders/Lit-Template` (286). That distinction matters in §4.7.

**The mask and normal are physically duplicated into every sibling bundle.** This is Addressables packing implicit dependencies, and it is byte-exact. Decoding `CryePantMask` and `CRYE_G3_MC_Normal_12` out of the M81, Gray, Marpat and Khaki bundles gives RGBA-buffer md5 `6dd0fda64da8e4f6` and `112b2461105bc1ec` in all four. Across the whole scan:

| shared map | copies | resolution | total bytes |
|---|---|---|---|
| `CryePantMask` | 20 | 1024 DXT5 m11 | 27,962,560 |
| `crye_g3_3` | 20 | 1024 DXT5 m11 | 27,962,560 |
| `CRYE_G3_MC_Normal_12` | 17 | 1024 DXT5 m11 (one copy is 2048 m12) | 27,962,480 |
| `CRYE_G3_MC_Normal_4` | 17 | 1024 DXT5 m11 (two copies are 2048 m12) | 32,156,784 |
| `crye_g3_1` | 10 | 1024 DXT5 m11 | 13,981,280 |
| `CRYE_G3_MC_Normal` | 10 | 1024 DXT5 m11 | 13,981,280 |
| `PatagoniaMask` | 13 | 2048 DXT5 m12 | 72,701,616 |
| `tShirtMask` | 15 | 2048 DXT5 m12 | 83,886,480 |
| `TShirt_Black_Normal` | 15 | 2048 DXT5 m12 | 83,886,480 |

You pay for the mask and normal once per colourway. A healthy Crye G3 pants bundle runs 2,864,355 to 3,921,077 bytes (2.7 to 3.7 MiB) for one 2,796,216-byte albedo. The 16 bundles under the `mi_crye_pants_` slug total 130,221,855 B (124.2 MiB); the four remaining pants colourways live under `cryeg3aor` slugs and bring the branch to 145,293,756 B (138.6 MiB).

#### The prefab does not reference the material bundle

The dive notes claimed the stripped prefab holds a hard PPtr into the material bundle. It does not, and the correction matters. Every `charactermods_stripped` prefab in the Crye pants family ships its `SkinnedMeshRenderer.m_Materials[0]` bound to `PPtr(m_FileID=2, m_PathID=-5343436609273763532)`. That is 40 out of 40 renderer slots across the 20 prefabs, identical in every one. External slot 2 of `CAB-f7ff1d55e1999e637520c7a0bd9e675d` resolves to `CAB-5b00af9b95ebb9aec2086aa927439000`, which is `weaponmodmaterials_assets_modplaceholdermaterial_26ab66620194ad9caab63da2f7a35e00.bundle`, material name `mod placeholder material`. The renderer ships bound to a placeholder.

The real link is an Addressables `AssetReference` GUID inside the `CharacterMod` MonoBehaviour, and the catalog resolves it to the `.mat`:

```
charactermods_stripped_assets_cryeg3pantsm81_8ce8cd8d4058059965ed3f56967444e7.bundle
  GameObject "CRYE G3 Pants M81"
  ├─ SKM_CRYE_Pants [SkinnedMeshRenderer] -> mod placeholder material   (replaced at runtime)
  ├─ SKM_CRYE_Pads  [SkinnedMeshRenderer] -> mod placeholder material
  ├─ Mesh SKM_CRYE_Pants, Mesh CRYE_PADS  (embedded in the prefab bundle)
  └─ MonoBehaviour CharacterMod { AssetReference GUID "283c187b01d6b6a46998563462bc9954" }
                                                       |
  catalog.bin @ 2117628:  283c187b01d6b6a46998563462bc9954 <uint32 21> "MI_CRYE_Pants_M81.mat"
```

Two consequences. First, a `charactermodmaterials`-only reskin cannot break the prefab, because the prefab bundle carries no dependency on it. Second, you cannot tell which camo a prefab wears by reading the prefab; you must resolve its GUID through `catalog.bin`.

Three record shapes turn up after the 32-hex GUID, and a working reader has to handle all three. The common one is a `uint32` length followed by the name inline, as above. The second is a pooled name, where those four bytes are a little-endian absolute offset into the string region instead: after GUID `962a4368bc5347245888cc1cd40b8151` they decode to `0x2056C4` = 2119364, exactly where `crye g3 aor 4.mat` sits, and that is how `cryeg3pantsaor2` resolves. The third is an interned path segment: `cryeg2shirtaor2`'s GUID `a2663966c44095c419b1be319c5f7f52` is followed by length 16 and the string `PBR_CRYE_AOR2_tx`, a folder name rather than a `.mat`. The rule that distinguishes a length from an offset is not established, so treat the magnitude test as a working hack, not a parser. This decoding resolved 67 of the 68 Crye prefabs to a `.mat` name; `cryeg2shirtaor2` is the one that lands on a folder.

### 4.2 Mechanism two: tint-only materials that bind no albedo

The plain T-shirt family carries its colour in the `_BaseColor` float4 and binds no base colour map at all. Seven materials have a null `_BaseColorMap` PPtr with only `_MaskMap` and `_NormalMap` live, all seven sharing `tShirtMask` and `TShirt_Black_Normal`, all seven on `Shader Graphs/ClothWind`. Every prefab mapping below was resolved through `catalog.bin` from the 28 shirt prefab GUIDs, and the tucked and untucked prefabs of one colour share a single GUID.

| material | bundle slug | `_BaseColor` (and `_Color`, identical) | wardrobe entries |
|---|---|---|---|
| `red` | `red_a86e649532437cb86d2b2b463c07cd11` | `0.2925, 0.1586, 0.1586, 1.0` | Shirt Tucked/Untucked (Red) |
| `RG` | `rg_c65be5f92a14a21f22e91c3487535e60` | `0.2961, 0.3113, 0.2717, 1.0` | Shirt Tucked/Untucked (RangerGreen) |
| `Blue` | `blue_9f253200ad03b4a1cfa927437d20cf00` | `0.1752, 0.2514, 0.2925, 1.0` | Shirt Tucked/Untucked (Blue) |
| `Greem` | `greem_083ad197c758fa63a67fe2807a5ce544` | `0.1686, 0.3098, 0.1529, 1.0` | Shirt Tucked/Untucked (Green) |
| `Brown` | `brown_dc2b56d91b68a46ff6ea37d76d55f3b9` | `0.3098, 0.2542, 0.1529, 1.0` | Shirt Tucked/Untucked (Brown) |
| `Gray` | `gray_3173a4c5631669b5f6a03fb699cd6463` | `0.2547, 0.2547, 0.2547, 1.0` | Shirt Tucked/Untucked (Gray) |
| `FDE` | `fde_3c211c4a2052258cbe1ebf25ce20126e` | `0.3585, 0.3174, 0.2587, 1.0` | Shirt Tucked/Untucked (FDE) |

`Greem` is a shipped typo and the wardrobe entry it drives is named Green. `MI_TShirt_3DMA_Snake` (unnumbered) is an eighth member of this class by accident: its `_BaseColorMap` is null and its `_BaseColor` is `0.7547, 0.7547, 0.7547, 1.0`, and its GUID `764202187fa24bc45975508bbf4c84c5` is what both `shirttucked(white)` and `shirtuntucked(white)` resolve to, so both White entries render as flat light grey rather than a white texture. Whether these floats are linear or gamma depends on the colour mode of the exposed Color node in the ClothWind graph, which is not readable from the bundle; treat them as raw shader input and match by eye or settle it with a runtime probe.

Adding a solid colour here is a two-number edit with no texture work, but it costs about 6,991,326 bytes (6.67 MiB) because the bundle still carries a full duplicate of the 2048 mask and the 2048 normal. The seven solid colours total 48,939,278 B (46.7 MiB) containing zero unique art. You cannot put a pattern on this family by editing floats, because the `_BaseColorMap` slot is unused. A patterned T-shirt must start from one of the seven family bundles that already carry a live albedo: `mi_tshirt_3dma_snake1`, `snake2`, `snake3`, `mi_tshirt_usa`, `red1`, `middleeast` or `stupid`. Note that `MI_TShirt_3DMA_Snake 3` is the one family member on `HDRP/Lit` rather than ClothWind, so step 6 of §4.7 applies to it.

### 4.3 Mechanism three: runtime `_BaseColorMap` override

The third mechanism keeps one material and swaps only the base colour texture at runtime. `CharacterMod` carries `textureModifer`, `materialBaseColourReference` and a `baseColour` texture PPtr. Per the banked scan of the `CharacterMod` MonoBehaviour across all 1,165 stripped bundles, 217 mods use it and `materialBaseColourReference` is the string `"_BaseColorMap"` in all 217 cases; a further 23 swap a whole material through `CharacterMod.material` plus `materialIndex`, and 927 do neither. In override mode the bundle contains a `Texture2D` and no `Material` at all, which `materials.tsv` confirms for every `cryeg3camo*`, `cryeg3tucked*`, `cryeg3aor2`, `cryeg3multicam` and `cryeg3multicamblack` bundle. The mask and normal come from whatever garment material is already on the renderer. Reading the raw MonoBehaviour bytes gives the grouping strings directly, checked on ten bundles spanning both group extremes:

```
charactermods_stripped_assets_cryeg3camo4_...       Compatibility "CRYEG3Shirt_Color"  ref "_BaseColorMap"
charactermods_stripped_assets_cryeg3camo21_...      Compatibility "CRYEG3Shirt_Color"  ref "_BaseColorMap"
charactermods_stripped_assets_cryeg3multicam_...    Compatibility "CRYEG3Shirt_Color"  ref "_BaseColorMap"
charactermods_stripped_assets_cryeg3aor2_...        Compatibility "CRYEG3Shirt_Color"  ref "_BaseColorMap"
charactermods_stripped_assets_cryeg3tuckedcamo4_..  Compatibility "CRYEG3_Color"       ref "_BaseColorMap"
charactermods_stripped_assets_cryeg3tuckedcamo21_.. Compatibility "CRYEG3_Color"       ref "_BaseColorMap"
charactermods_stripped_assets_cryeg3tuckedaor2_...  Compatibility "CRYEG3_Color"       ref "_BaseColorMap"
```

Both groups hold 21 prefabs. The shirt group is `cryeg3camo4` through `cryeg3camo21` carrying `T_Shirt_CamoA_D` through `T_Shirt_CamoI_D` and `T_Shirt_ColorA_D` through `T_Shirt_ColorI_D`, all 2048x2048 DXT5 m12, plus `cryeg3aor2` (`AOR2`), `cryeg3multicam` (`Multicam2`) and `cryeg3multicamblack` (`mcb_shirt`), all 2048 DXT1 m12. The tucked-pants group mirrors it exactly with `cryeg3tuckedcamo4` through `21` carrying `T_Pants_CamoA_D` through `T_Pants_ColorI_D` at 1024x1024 DXT5 m11, plus `cryeg3tuckedaor2` (`AOR 2`, note the space), `cryeg3tuckedmulticam` (`Pant_Multicam2`) and `cryeg3tuckedmulticamblack` (`mcb_pants`), all 1024 DXT1 m11. This is the cheapest branch to author into: one texture, no material edit, no shader work, and the texture sits in the `charactermods_stripped` bundle rather than a materials bundle.

### 4.4 The standalone camo pattern library

A distinct set of textures is named after the pattern itself rather than after the garment. Twenty-nine such names ship as 60 physical texture objects totalling 212,512,416 bytes (202.7 MiB), every one of them 2048x2048 with 12 mips, 44 DXT1 and 16 DXT5.

The DXT5 split is narrower than that ratio suggests. Fifteen of the 16 DXT5 copies sit in `mi_airframe_helmet*` bundles; the sixteenth is `FDE` inside `weaponmodmaterials_assets_emagmag`. Every Ops-Core FAST MT helmet shell, every cover, every jacket and every shirt takes DXT1. On HDRP/Lit with `_SurfaceType` 0 and `_AlphaCutoffEnable` 0, both verified on those helmet materials, base-colour alpha is not sampled, so each Airframe helmet colourway spends 5,592,432 bytes where 2,796,216 would do.

| pattern texture | copies | formats | consuming materials |
|---|---|---|---|
| `AOR1` | 3 | DXT1, DXT5 | `MI_Airframe_Cover 2`, `MI_Airframe_Helmet AOR1`, `MI_Jacket_Patagonia 2` |
| `AOR2` | 5 | DXT1, DXT5 | `MI_Airframe_Cover 3`, `MI_Airframe_Helmet AOR2`, `MI_Opscore_FAST_MT_Cover_AOR2`, `M_ShirtAOR2`, plus a loose copy embedded in `charactermods_stripped_assets_cryeg3aor2` |
| `Coyote Brown` | 5 | DXT1, DXT5 | `MI_Airframe_Cover 4`, `MI_Airframe_Helmet Coyote`, `MI_Jacket_Patagonia 3`, `MI_Opscore_FAST_MT_Cover_CoyoteBrown`, `MI_Opscore_FAST_MT_Helmet_CoyoteBrown` |
| `Multicam` | 1 | DXT5 | `MI_Airframe_Helmet Multicam` |
| `Multicam Alpine` | 2 | DXT1, DXT5 | `MI_Airframe_Helmet Multicam Alpine`, `MI_Opscore_FAST_MT_Cover_MCAlpine` |
| `Multicam Arid` | 5 | DXT1, DXT5 | `MI_Airframe_Cover 6`, `MI_Airframe_Helmet Multicam Alpine 1`, `MI_Jacket_Patagonia 5`, `MI_Opscore_FAST_MT_Cover_MCArid`, `MI_Rugby_Shirt_FDE 1` |
| `Multicam Jungle` | 5 | DXT1, DXT5 | `MI_Airframe_Cover 7`, `MI_Airframe_Helmet Multicam Jungle`, `MI_Jacket_Patagonia 7`, `MI_Opscore_FAST_MT_Cover_MCJungle`, `MI_Rugby_Shirt_FDE 2` |
| `Multicam Black` | 1 | DXT1 | `MI_Jacket_Patagonia 6` |
| `Multicam Black 1` / `Multicam Black 2` | 1 each | DXT5 | `MI_Airframe_Helmet Multicam Black 1` / `... 2` |
| `Ranger Green` | 6 | DXT1, DXT5 | `MI_Airframe_Cover 8`, `MI_Airframe_Helmet RG`, `MI_Jacket_Patagonia 8`, `MI_Opscore_FAST_MT_Cover_RG`, `MI_Opscore_FAST_MT_Helmet_RG`, `MI_Rugby_Shirt_FDE 3` |
| `Tan` | 5 | DXT1, DXT5 | `MI_Airframe_Cover 9`, `MI_Airframe_Helmet Tan`, `MI_Opscore_FAST_MT_Cover_TAN`, `MI_Opscore_FAST_MT_Helmet_Tan`, `MI_Rugby_Shirt_FDE 4` |
| `M81` | 1 | DXT1 | `MI_Opscore_FAST_MT_Cover_M81` |
| `Tiger Stripe Desert` | 2 | DXT1 | `MI_Jacket_Patagonia 9`, `MI_Opscore_FAST_MT_Cover_TSD` |
| `Tiger Stripe Jungle` | 1 | DXT1 | `MI_Jacket_Patagonia 10` |
| `Tiger Stripe Woodland` | 1 | DXT1 | `MI_Opscore_FAST_MT_Cover_TS` |
| `AMCU` | 1 | DXT1 | `MI_Opscore_FAST_MT_Cover_AMCU` |
| `MTP` | 1 | DXT1 | `MI_Opscore_FAST_MT_Cover_MTP` |
| `DCU` | 1 | DXT1 | `MI_Jacket_Patagonia 4` |
| `FDE` | 2 | DXT1, DXT5 | `MI_Opscore_FAST_MT_Helmet_FDErt` (DXT1), and `EMAG mag` in `weaponmodmaterials` (DXT5) |
| `Black` | 2 | DXT1 | `MI_Airframe_Cover 10`, `MI_Opscore_FAST_MT_Cover_BLK` |
| `White` | 1 | DXT1 | `MI_PCU_Grey 2` |
| `Blue` | 1 | DXT1 | `MI_Rugby_Shirt_FDE 5` |
| `Brown` | 1 | DXT1 | `MI_Rugby_Shirt_FDE 6` |
| `Black, Tan Helmet` | 1 | DXT1 | `MI_Opscore_FAST_MT_Helmet_BLK2` |
| `Black Battleworn` | 1 | DXT5 | `MI_Airframe_Helmet BLk 2` |
| `Coyote Brown Battleworn` | 1 | DXT5 | `MI_Airframe_Helmet Coyote Worn` |
| `Ranger Green Battleworn` | 1 | DXT5 | `MI_Airframe_Helmet RG 1` |
| `Tan Battleworn` | 1 | DXT5 | `MI_Airframe_Helmet Tan 1` |

The Battleworn set exists only on Airframe helmet shells, four patterns deep, all DXT5. There is no Battleworn variant for any cover, jacket or shirt, so a wear pass on the rest of the library is open ground.

Two naming hazards live in this table, and only one of them reaches the wardrobe. `MI_Airframe_Helmet Multicam Alpine 1` binds `Multicam Arid`, not Alpine, but the prefab that loads it is `cryeairframemulticamaridvariant`, so the wardrobe entry is correctly named and the lie stops at the material. `MI_Opscore_FAST_MT_Cover_AOR1` does not bind `AOR1` at all; it binds `SM_Opscore_FAST_MT_Cover_1_MI_Opscore_FAST_MT_Cover_MC_1_BaseMap`, and `MI_Opscore_FAST_MT_Cover_MCB` binds the ` 1` copy of that same name.

A fourth, larger vocabulary uses the form `<Item> <Colourway>` for the plate carrier and pouch branch (`Vest AOR1`, `Pouch M81`, `IFAK MCB`, `Belt OD Green`, `Radio GRAY`, `TenSpeed BLK`, `Shingle OD Green`, `Vest ODG`, `12x5 AOR2`, `6x9 M81`, and so on). Those are covered in the gear section; they follow the same one-bundle-per-colourway rule but sit on `MilkShaders/Lit-Template` rather than HDRP or ClothWind.

### 4.5 A shared name is not a shared file

This is the single most expensive assumption to get wrong. Within one garment family, a shared texture name does mean a shared file, verified by md5. Across garment families it does not. Six bundles ship a 2048x2048 12-mip texture named `Ranger Green`; decoding all six gives six different images, because each is a Ranger Green fill baked into that garment's own UV layout:

```
"Ranger Green" decoded, md5 of the RGBA pixel buffer, from the live bundles
  mi_airframe_cover8            DXT1  2,796,216 B  c520345d651ef154   helmet cover shell + straps
  mi_airframe_helmetrg          DXT5  5,592,432 B  39eb29f332794761   helmet shell, rails, accessories
  mi_jacket_patagonia8          DXT1  2,796,216 B  7c7abe099a96f121   jacket panels
  mi_opscore_fast_mt_cover_rg   DXT1  2,796,216 B  99ce517bea093958   Ops-Core cover panels
  mi_opscore_fast_mt_helmet_rg  DXT1  2,796,216 B  6bd1af4aee1a0f95   Ops-Core shell
  mi_rugby_shirt_fde3           DXT1  2,796,216 B  36b8c4a8a1502e9b   rugby shirt panels
mean absolute per-channel difference over all 15 pairs: 22.6 to 36.9
```

The same holds inside a family when the artist reused a filename. `crye retexture_MI_CRYE_Pants_MC_BaseMap` exists under that exact name in four bundles; the M81, Gray and Marpat copies decode to md5 `9f8b5e3a01f1afa0`, `0e1236c032c84090` and `ef1875d1b7c62aa5`, and the fourth is the broken 1024 DTS copy. Three different camos, one filename, and the filename says MC. `crye retexture_MI_CRYE_LS_BaseMap` does the same across `mi_crye_ls_dts`, `_gray`, `_m81` and `_marpat`.

The rule that follows is that a full camo swap is N bundle edits **and** N distinct paint jobs, one per UV layout, not one texture copied N times. Adding your pattern everywhere `Ranger Green` currently appears means six bundles and six separate layouts. Key every entry in your manifest on the **bundle filename slug**, the part between `charactermodmaterials_assets_` and the 32-hex hash, never on the material or texture name. Material `m_Name` is not unique either: 61 material names repeat across `charactermodmaterials` bundles, including `crye g3 aor 3` and `crye g3 aor 4` in two bundles each with different albedos, `MI_Jeans_LEVIS 1` in three, and `MI_PCU_Grey 1`, `MI_PCU_Grey 2`, `MI_Rugby_Shirt_FDE 1`, `MI_Rugby_Shirt_FDE 2`, `Vest AOR1` and `Pouch M81` in two each.

Compression is not stable across copies either. `Multicam Arid` ships DXT1 in four bundles and DXT5 in the Airframe helmet bundle. `CRYE_G3_MC_Normal_12` is 1024 DXT5 m11 in sixteen bundles and 2048 DXT5 m12 in the DTS bundle. Always read the donor's `m_TextureFormat`, `m_Width` and `m_MipCount` and write them back; do not assume the family default.

### 4.6 The Crye G3 trees

These are the two deepest families in the game. Every prefab-to-material row below was resolved through `catalog.bin`, and every material-to-texture row from the material's own `m_TexEnvs`. The source project groups the materials by texture folder, and the folder is recoverable from each bundle's single `m_Container` entry, which is what resolves the naming chaos in the `crye g3 aor N` set.

The reliable garment discriminator is the mask name: `crye_g3_3` is the G2 long-sleeve shirt, `CryePantMask` is the G3 pants, `crye_g3_1` is the G3 cut top. The normal map is a second signal, suffix `_4` for the shirt, `_12` for the pants, unsuffixed for the cut top. The base colour suffix is a third and weaker one: `_1` on every shirt albedo, `_2` or `_9` or `_10` on the pants, unsuffixed on the cut.

```
CRYE G3 PANTS   20 wardrobe entries, 20 material bundles
                16 under the mi_crye_pants_ slug = 130,221,855 B (124.2 MiB)
                plus 4 under cryeg3aor slugs     = 145,293,756 B (138.6 MiB) for the branch
  shared by all 20:  _MaskMap CryePantMask 1024 DXT5 m11 (byte-identical, md5 verified)
  _NormalMap CRYE_G3_MC_Normal_12 is 1024 DXT5 m11 in 16 bundles, 2048 DXT5 m12 in the DTS bundle,
             and replaced by cryeg2g3_MI_CRYE_Pants_MC_Normal 4096 DXT5 m13 in the three WORN bundles
  shader: Shader Graphs/ClothWind      m_ValidKeywords: ['_DISABLE_SSR_TRANSPARENT']

  wardrobe slug (charactermods_stripped)  material                 material bundle slug     _BaseColorMap
  cryeg3pantsmc                           MI_CRYE_Pants_MC         mi_crye_pants_mc         CRYE_G3_MC_BaseColor_9              2048 DXT1 m12
  cryeg3pantsblk                          MI_CRYE_Pants_MC 1       mi_crye_pants_mc1        CRYE_G3_Black_BaseColor_9           2048 DXT1 m12
  cryeg3pantsmcalpine                     MI_CRYE_Pants_MCA        mi_crye_pants_mca        MI_CRYE_Pants_Alpine_BaseMap        2048 DXT1 m12
  cryeg3pantsmcb                          MI_CRYE_Pants_MCB        mi_crye_pants_mcb        MI_CRYE_Pants_MCB_BaseMap           2048 DXT1 m12
  cryeg3pantsmctropic                     MI_CRYE_Pants_Tropic     mi_crye_pants_tropic     MI_CRYE_Pants_Tropic_BaseMap        2048 DXT1 m12
  cryeg3pantsflecktarn                    MI_CRYE_Pants_Flecktarn  mi_crye_pants_flecktarn  MI_CRYE_Pants_Flecktarn_BaseMap     2048 DXT1 m12
  cryeg3pantskhaki                        MI_CRYE_Pants_Khaki      mi_crye_pants_khaki      MI_CRYE_Pants_Khaki_BaseMap         2048 DXT1 m12
  cryeg3pantsrg                           MI_CRYE_Pants_RG         mi_crye_pants_rg         MI_CRYE_Pants_RG_BaseMap            2048 DXT1 m12
  cryeg3pantsm81                          MI_CRYE_Pants_M81        mi_crye_pants_m81        crye retexture_..._Pants_MC_BaseMap 2048 DXT1 m12  md5 9f8b5e3a
  cryeg3pantswolfgray                     MI_CRYE_Pants_Gray       mi_crye_pants_gray       crye retexture_..._Pants_MC_BaseMap 2048 DXT1 m12  md5 0e1236c0
  cryeg3pantsmarpat                       MI_CRYE_Pants_Marpat     mi_crye_pants_marpat     crye retexture_..._Pants_MC_BaseMap 2048 DXT1 m12  md5 ef1875d1
  cryeg3pantsmarpatdesert                 MI_CRYE_Pants_Marpat 1   mi_crye_pants_marpat1    crye retexture_..._Pants_MC_BaseMap 1  2048 DXT1 m12
  cryeg3pantsdeserttigerstripe            MI_CRYE_Pants_DTS        mi_crye_pants_dts        crye retexture_..._Pants_MC_BaseMap 1024 DXT1 m1, not streamed  [BROKEN, normal is 2048 here]
  cryeg3pantsaor1                         crye g3 aor 2            cryeg3aor2               CRYE_G3_AOR1_BaseColor_9            2048 DXT1 m12
  cryeg3pantsaor2                         crye g3 aor 4            cryeg3aor4               CRYE_G3_AOR2_BaseColor_9            2048 DXT1 m12
  cryeg3pantsmcarid                       crye g3 aor 7            cryeg3aor7               CRYE_G3_MC_Arid_BaseColor_10        2048 DXT1 m12
  cryeg3pantstigerstripe                  crye g3 aor 6            cryeg3aor6               CRYE_G3_TigerStripe_BaseColor_2     2048 DXT1 m12
  veteran sub-branch, own 4096 normal cryeg2g3_MI_CRYE_Pants_MC_Normal 4096 DXT5 m13
  cryeg3pantsveteranmc                    MI_CRYE_Pants_MCWORN     mi_crye_pants_mcworn     cryeg2g3_..._Pants_MC_BaseMap       2048 DXT1 m12   bundle 20,117,336 B
  cryeg3pantsveteranmcb                   MI_CRYE_Pants_MCBWORN    mi_crye_pants_mcbworn    cryeg2g3_..._Pants_MC_BaseMap       2048 DXT1 m12   bundle 19,960,946 B
  cryeg3pantsveteranaor1                  MI_CRYE_Pants_AOR1WORN   mi_crye_pants_aor1worn   cryeg2g3_..._Pants_MC_BaseMap       1024 DXT1 m1, not streamed  [BROKEN]  bundle 35,620,124 B

  parallel override branch, CharacterMod Compatibility "CRYEG3_Color", 21 prefabs
  cryeg3tuckedcamo4..21    -> T_Pants_CamoA_D .. CamoI_D, T_Pants_ColorA_D .. ColorI_D   1024 DXT5 m11, embedded in the prefab bundle
  cryeg3tuckedaor2         -> "AOR 2"           1024 DXT1 m11
  cryeg3tuckedmulticam     -> "Pant_Multicam2"  1024 DXT1 m11
  cryeg3tuckedmulticamblack-> "mcb_pants"       1024 DXT1 m11
```

```
CRYE G2/G3 SHIRT   20 sleeves-down entries + 17 rolled entries, 20 material bundles
                   16 under the mi_crye_ls slug = 124,440,597 B (118.7 MiB)
                   plus 4 under cryeg3aor slugs = 138,142,500 B (131.7 MiB) for the branch
  shared by all 20:  _MaskMap crye_g3_3 1024 DXT5 m11
  _NormalMap CRYE_G3_MC_Normal_4 is 1024 DXT5 m11 in 15 bundles, 2048 DXT5 m12 in the M81 and DTS
             bundles, and replaced by cryeg2g3_MI_CRYE_LS_Normal 4096 DXT5 m13 in the three WORN bundles
  shader: Shader Graphs/ClothWind

  wardrobe slug (and its cryeg2rolled* twin)  material                material bundle slug   _BaseColorMap
  cryeg2shirtmc            / cryeg2rolledmc             MI_CRYE_LS             mi_crye_ls             CRYE_G3_MC_BaseColor_1           2048 DXT1 m12
  cryeg2shirtblk           / cryeg2rolledblk            MI_CRYE_LS 1           mi_crye_ls1            CRYE_G3_Black_BaseColor_1        2048 DXT1 m12
  cryeg2shirtmcalpine      / cryeg2rolledmcalpine       MI_CRYE_LS MCA         mi_crye_lsmca          MI_CRYE_LS_Alpine_BaseMap        2048 DXT1 m12
  cryeg2shirtmcb           / cryeg2rolledmcb            MI_CRYE_LS MCB         mi_crye_lsmcb          MI_CRYE_LS_MCB_BaseMap           2048 DXT1 m12
  cryeg2shirtmctropic      / cryeg2rolledmctropic       MI_CRYE_LS_Tropic      mi_crye_ls_tropic      MI_CRYE_LS_Tropic_BaseMap        2048 DXT1 m12
  cryeg2shirtflecktarn     / cryeg2rolledflecktarn      MI_CRYE_LS_Flecktarn   mi_crye_ls_flecktarn   MI_CRYE_LS_Flecktarn_BaseMap     2048 DXT1 m12
  cryeg2shirtkhaki         / cryeg2rolledkhaki          MI_CRYE_LS_Khaki       mi_crye_ls_khaki       MI_CRYE_LS_Khaki_BaseMap         2048 DXT1 m12
  cryeg2shirtrg            / cryeg2rolledrg             MI_CRYE_LS_RG          mi_crye_ls_rg          MI_CRYE_LS_RG_BaseMap            2048 DXT1 m12
  cryeg2shirtwolfgray      / cryeg2rolledwolfgray       MI_CRYE_LS_Gray        mi_crye_ls_gray        crye retexture_MI_CRYE_LS_BaseMap    2048 DXT1 m12
  cryeg2shirtmarpat        / cryeg2rolledmarpat         MI_CRYE_LS_Marpat      mi_crye_ls_marpat      crye retexture_MI_CRYE_LS_BaseMap    2048 DXT1 m12
  cryeg2shirtmarpatdesert  / cryeg2rolledmarpatdesert   MI_CRYE_LS_Marpat 1    mi_crye_ls_marpat1     crye retexture_MI_CRYE_LS_BaseMap 1  2048 DXT1 m12
       (a third prefab, cryeg2marpatdesert, resolves to the same material)
  cryeg2shirtm81           / cryeg2rolledm81            MI_CRYE_LS_M81         mi_crye_ls_m81         crye retexture_MI_CRYE_LS_BaseMap    1024 DXT1 m1, not streamed  [BROKEN, normal is 2048 here]
  cryeg2shirtdeserttigerstripe / cryeg2rolleddeserttigerstripe
                                                        MI_CRYE_LS_DTS         mi_crye_ls_dts         crye retexture_MI_CRYE_LS_BaseMap    1024 DXT1 m1, not streamed  [BROKEN, normal is 2048 here]
  cryeg2shirtaor1          / cryeg2rolledaor1           crye g3 aor 1          cryeg3aor1             CRYE_G3_AOR1_BaseColor_1         2048 DXT1 m12
  cryeg2shirtaor2          (no rolled twin)             crye g3 aor 3          cryeg3aor31            CRYE_G3_AOR2_BaseColor_1         2048 DXT1 m12
       (inference: this prefab's GUID resolves to the interned folder segment "PBR_CRYE_AOR2_tx" rather
        than to a .mat name, and cryeg3aor31 is the only member of that folder carrying the shirt mask
        crye_g3_3; an offset-accurate catalog parse would confirm it)
  cryeg2shirtmcarid        (no rolled twin)             crye g3 aor 8          cryeg3aor8             CRYE_G3_MC_Arid_BaseColor_1      2048 DXT1 m12
  cryeg2shirttigerstripe   (no rolled twin)             crye g3 aor 5          cryeg3aor5             CRYE_G3_TigerStripe_BaseColor_1  2048 DXT1 m12
  veteran sub-branch, own 4096 normal cryeg2g3_MI_CRYE_LS_Normal 4096 DXT5 m13
  cryeg2shirtveteranmc     / cryeg2rolledveteranmc      MI_CRYE_LS_MCWORN      mi_crye_ls_mcworn      cryeg2g3_MI_CRYE_LS_BaseMap      2048 DXT1 m12
  cryeg2shirtveteranmcb    / cryeg2rolledveteranmcb     MI_CRYE_LS_MCBWORN     mi_crye_ls_mcbworn     cryeg2g3_MI_CRYE_LS_BaseMap      2048 DXT1 m12
  cryeg2shirtveteranaor1   / cryeg2rolledaor1veteran    MI_CRYE_LS_AOR1WORN    mi_crye_ls_aor1worn    cryeg2g3_MI_CRYE_LS_BaseMap      4096 DXT1 m13

  the "cut" top is a third garment sharing the vocabulary, 10 prefabs, mask crye_g3_1, normal CRYE_G3_MC_Normal
  cryeg3cutaor1 -> crye g3 aor 3 (cryeg3aor3)          cryeg3cutaor2variant -> crye g3 cut aor 4
  cryeg3cutblkvariant -> crye g3 cut aor 5             cryeg3cutmcaridvariant -> crye g3 cut aor 6
  cryeg3cutmcvariant -> crye g3 cut aor 7              cryeg3cuttigerstripevariant -> crye g3 cut aor 8
  cryeg3cutmcb -> crye g3 cut MCB                      cryeg3cutveteranaor1 -> crye g3 aor 4 (cryeg3aor41, 4096 basemap)
  cryeg3cutveteranmc -> crye g3 MC                     cryeg3cutveteranmcb -> crye g3 MCB
  (the catalog gives only the .mat name for the two "crye g3 aor N" rows, and that name lives in two
   bundles each; picking the cryeg3aor3 / cryeg3aor41 bundle is inference from the crye_g3_1 cut mask)

  parallel override branch, CharacterMod Compatibility "CRYEG3Shirt_Color", 21 prefabs
  cryeg3camo4..21          -> T_Shirt_CamoA_D .. CamoI_D, T_Shirt_ColorA_D .. ColorI_D   2048 DXT5 m12, embedded in the prefab bundle
  cryeg3aor2               -> "AOR2"        2048 DXT1 m12
  cryeg3multicam           -> "Multicam2"   2048 DXT1 m12
  cryeg3multicamblack      -> "mcb_shirt"   2048 DXT1 m12
```

The `m_Container` paths explain the `crye g3 aor N` numbering, which encodes nothing about AOR. All 50 Crye materials sit under `Assets/1Assets/1CGTrader/3DMA/Uniform_CRYE/Textures/`, one folder per pattern, each holding whichever of the shirt, pants and cut variants that pattern happens to have:

```
CRYE WORN/                       MI_CRYE_LS_AOR1WORN, MI_CRYE_Pants_AOR1WORN
CRYE WORN/Multicam/              MI_CRYE_LS_MCWORN, MI_CRYE_Pants_MCWORN, crye g3 MC
CRYE WORN/Multicam Black/        MI_CRYE_LS_MCBWORN, MI_CRYE_Pants_MCBWORN, crye g3 MCB
PBR  CRYE MARPAT/                MI_CRYE_LS_Marpat, LS_Marpat 1, Pants_Marpat, Pants_Marpat 1  (note the double space)
PBR CRYE DESERT TIGER STRIPE/    MI_CRYE_LS_DTS, MI_CRYE_Pants_DTS
PBR CRYE FLECKTARN/              MI_CRYE_LS_Flecktarn, MI_CRYE_Pants_Flecktarn
PBR CRYE KHAKI/                  MI_CRYE_LS_Khaki, MI_CRYE_Pants_Khaki
PBR CRYE M81/                    MI_CRYE_LS_M81, MI_CRYE_Pants_M81
PBR CRYE MCA/                    MI_CRYE_LS MCA, MI_CRYE_Pants_MCA
PBR CRYE MCB/                    MI_CRYE_LS MCB, MI_CRYE_Pants_MCB, crye g3 cut MCB
PBR CRYE MCT/                    MI_CRYE_LS_Tropic, MI_CRYE_Pants_Tropic
PBR CRYE RG/                     MI_CRYE_LS_RG, MI_CRYE_Pants_RG
PBR WOLF GRAY/                   MI_CRYE_LS_Gray, MI_CRYE_Pants_Gray
PBR_CRYE_AOR1_tx/                crye g3 aor 1, aor 2, aor 3 (cryeg3aor3), aor 4 (cryeg3aor41, 4096 cut basemap)
PBR_CRYE_AOR2_tx/                crye g3 aor 3 (cryeg3aor31), aor 4 (cryeg3aor4), cut aor 4
PBR_CRYE_Black_tx/               MI_CRYE_LS 1, MI_CRYE_Pants_MC 1, crye g3 cut aor 5
PBR_CRYE_MC_tx/                  MI_CRYE_LS, MI_CRYE_Pants_MC, crye g3 cut aor 7
PBR_CRYE_MC_Arid_tx/             crye g3 aor 7, aor 8, cut aor 6
PBR_CRYE_TigerStripe_tx/         crye g3 aor 5, aor 6, cut aor 8
```

`crye g3 aor 3` and `crye g3 aor 4` each appear once in the `AOR1` folder and once in the `AOR2` folder, with different albedos, which is why the bundle slugs carry a disambiguating trailing digit (`cryeg3aor3` versus `cryeg3aor31`, `cryeg3aor4` versus `cryeg3aor41`). Resolve by slug.

### 4.7 Authoring a new camo pattern

Steps 2, 4, 7 and 9 below were executed against `mi_crye_pants_khaki` and the numbers quoted are from that run. Order matters.

1. **Pick a donor whose branch shares one mask and normal and ships 2048 albedos.** The clean ones are Crye pants (16 bundles under the slug), Crye long-sleeve (16), Patagonia (11 bundles, every albedo 2048 DXT1 over `PatagoniaMask` plus `Patagonia_Black_Normal`), Rugby (10 bundles, every albedo 2048 DXT1 over `RugbyShirtMask` plus `Rugby_shirt_Normal`) and Boonie (6 bundles, 2048 DXT1 albedos, but on `HDRP/Lit` with BC7 mask and normal, so read step 6). Avoid the veteran sub-branch (4096 normals, 19 to 36 MB bundles) and avoid the `M_` NPC branch, whose masks are per-variant so a new pattern there needs two textures rather than one. Copy the live bundle out of `D:\Games\OPERATOR\OPERATOR_Data\StreamingAssets\aa\StandaloneWindows64` before touching anything; Steam file verification is your undo, and a depot update will restore stock bundles without warning.

2. **Extract the donor albedo as your UV layout.** Every colourway of a garment binds the identical mask and normal at scale `(1,1)` offset `(0,0)`, so the layout is shared across the family and any sibling's base map is a valid template. Extract the mask too; its green channel is AO and shows every seam, which makes alignment cheap.

   ```python
   import UnityPy
   UnityPy.config.FALLBACK_UNITY_VERSION = "6000.3.8f1"   # bundles are version stripped
   env = UnityPy.load(r"...\charactermodmaterials_assets_mi_crye_pants_khaki_<hash>.bundle")
   for o in env.objects:
       if o.type.name == "Texture2D":
           d = o.read()
           print(d.m_Name, d.m_Width, d.m_Height, int(d.m_TextureFormat), d.m_MipCount)
           d.image.save(f"{d.m_Name}.png")
   ```

3. **Paint at the donor's exact resolution.** Keep it opaque; the garment albedos are DXT1 and have no alpha. Do not bake lighting or AO into the albedo, both already live in the mask. If your pattern is meant to appear on several garments, remember §4.5: each garment needs its own bake against its own layout.

4. **Write it back with the format and mip count restored.** The naive `d.image = img; d.save()` sets `m_MipCount` to 1 and inlines a single level, which reads in game as a noisy, shimmering camo rather than as a broken tool. Measured on `MI_CRYE_Pants_Khaki_BaseMap`: the naive path gives `m_MipCount 1` and 2,097,152 inline bytes; `set_image(..., mipmap_count=12)` gives `m_MipCount 10` and 2,796,200 bytes against the shipped 2,796,216, because UnityPy stops the chain at the 4x4 DXT block and omits the padded 2x2 and 1x1 levels. That 16-byte shortfall is harmless. Always pass the donor's own format back rather than letting UnityPy choose.

   ```python
   from PIL import Image
   img = Image.open("new_pattern.png").convert("RGBA")
   d.set_image(img, target_format=d.m_TextureFormat, mipmap_count=12)
   d.save()
   open("out.bundle", "wb").write(env.file.save(packer="lz4"))
   ```

   The four shipped 1024/mip1 albedos (`MI_CRYE_LS_M81`, `MI_CRYE_LS_DTS`, `MI_CRYE_Pants_DTS`, `MI_CRYE_Pants_AOR1WORN`) carry exactly the signature this mistake produces, and they are also the only Crye albedos with `m_StreamData` cleared while every healthy sibling streams from the `.resS`. That pair of symptoms is what an object-level re-import leaves behind. It is inference from the artefact shape, not established from a build log.

5. **Do not touch the Material, the Shader objects, or `m_SavedProperties`.** Every material bundle ships its own compiled shaders with `m_FileID = 0` on the material's shader PPtr, so the binding is internal and survives a texture swap. There are three `Shader` objects per bundle, not one: the real shader plus its fallback twins, `Hidden/Shader Graph/FallbackError` and `Hidden/Core/FallbackError` on ClothWind bundles, `Hidden/HDRP/FallbackError` and `Hidden/Core/FallbackError` on HDRP/Lit ones. All of them read back with `m_Name == ''` and the real name only in `m_ParsedForm.m_Name`, so the usual `mat.shader = Shader.Find(mat.shader.name)` fix cannot work here; `Shader.Find("")` returns null. Leave the binding alone.

6. **If you must add a texture to a slot that was empty, check `m_ValidKeywords` first.** `HDRP/Lit` branches on `_MASKMAP`, `_NORMALMAP` and `_NORMALMAP_TANGENT_SPACE` at compile time, so setting `_MaskMap` on a material whose keyword list lacks `_MASKMAP` binds the texture and changes nothing. The helmet, cover, Boonie and `MI_TShirt_3DMA_Snake 3` materials carry `['_DISABLE_SSR_TRANSPARENT', '_MASKMAP', '_NORMALMAP', '_NORMALMAP_TANGENT_SPACE']`. Materials on `Shader Graphs/ClothWind` carry only `['_DISABLE_SSR_TRANSPARENT']`, with the HDRP keywords parked in `m_InvalidKeywords` because the graph never declares them, and they read their samplers unconditionally. So the problem is confined to the `HDRP/Lit` and `MilkShaders/Lit-Template` branches. It does not exist on ClothWind garments, and Boonie hats are the trap, because they look like clothing and are not.

7. **Rename the output to the current live filename.** The 32-hex suffix is the Addressables content hash and it changes with the game build, so re-glob the live directory rather than hardcoding it. Nothing validates your file: the setup is fully local with a single `catalog.bin` and no certificate handler, and the 16-byte content hash present in the catalog for 4,967 of the 4,989 bundles is the remote-caching hash, not verified for a local `LoadFromFileAsync`. The per-bundle CRC column reads zero wherever it could be located, which was 4,903 of the 4,979 records the anchor-on-file-size method resolved; the 76 nonzero readings are one-off values inside the 241 ambiguous matches, so the working conclusion is that CRC checking was off at build time, not that some bundles are checked. Repacking inflates the file because UnityPy's LZ4 is weaker than the shipped packing and the edited texture migrates out of the `.resS`; measured at +44% on the Khaki bundle with real art, and +42% to +57% on 12x5 BLK. Budget about 1.5x disk per edited bundle.

   ```powershell
   Get-ChildItem 'D:\Games\OPERATOR\OPERATOR_Data\StreamingAssets\aa\StandaloneWindows64' `
     -Filter 'charactermodmaterials_assets_mi_crye_pants_khaki_*.bundle'
   ```

8. **Accept that you are replacing, not adding.** Each of the 1,022 single-material bundles exposes exactly one addressable key, its `.mat` path, and the catalog is a fixed binary with absolute offsets into a pooled string region. A file swap gives you N-for-N substitution; every camo you add costs one you delete. Adding a new wardrobe entry needs a MelonLoader mod that registers a new `CharacterMod` with its own `AssetReference` and `Compatibility` string, which is a separate and larger job. Reskins should also be client-side only, since every client loads the material from its own copy of the bundle and no texture data crosses the wire, but that is inference from the load path and needs a live-session probe to confirm.

9. **Verify with UnityPy before launching.** Reload the output and confirm the object list is intact (the `AssetBundle` object, the `Material`, all three `Shader` objects, every `Texture2D`), that `m_MipCount` and `m_TextureFormat` match the donor, and that the single `m_Container` entry still names the original `.mat`. Then check it in the loadout cabinet rather than in a mission, because iteration there is far faster.

The cheapest high-value target is the four broken albedos: repaint `MI_CRYE_LS_M81`, `MI_CRYE_LS_DTS`, `MI_CRYE_Pants_DTS` and `MI_CRYE_Pants_AOR1WORN` at 2048 with a full mip chain. That touches no prefab and no catalog and is a pure quality gain.

Two targets that earlier notes listed as free wins are not. The dive claimed `MI_PCU_Coyote`, `MI_PCU_FDE` and `MI_PCU_OD` render as the same grey jacket because all three bind `PCU_Jacket_HoodDown_MI_PCU_Grey_BaseMap`, and that `MI_Rugby_Shirt_COYOTE` is a pixel duplicate of `MI_Rugby_Shirt_FDE`. Decoding says otherwise. That PCU texture name appears in four bundles carrying four different images with four different mean colours (`mi_pcu_coyote` 77/57/35, `mi_pcu_fde` 88/73/56, `mi_pcu_od` 54/55/42, `mi_pcu_grey11` 37/37/36), and the two Rugby copies differ too (71/54/38 against 49/44/38). They are distinct colourways that happen to share a filename, which is §4.5 biting whoever wrote §4.5. Verify by md5, never by name.

## 5. The character gear tree

Everything a player wears lives in one Addressables family: `charactermodmaterials`. The sweep found **1,025 bundles carrying 1,036 materials**, which the tree builder groups into **619 items across 22 categories**. One bundle holds one material in 1,022 cases. Three break it: `charactermodmaterials_assets_jpc_v2_ed7ad93ca01d445dfdaa32fb6b09b8fe.bundle` carries nine materials (`JPC_sdr`, `ANPRC152_sdr`, `TrellisWare_sdr`, `Patch_3DMA_sdr`, `M4Pmag_sdr`, `BackPanel_Spiritus_sdr`, `MechanicxGloves_Left_Black_sdr`, `Roll1_sdr`, `Miscellaneous_01_sdr`), `..._delta_de4fb9491fd759a47aec308b989fb9cb.bundle` carries three, and `..._gatorz_folded_c6db774468a9052d3ca0a99e4f48c855.bundle` carries two. Everywhere else, one bundle is one material.

The exhaustive listing is `TEXTURE_TREE.txt`, character branch at lines 6 to 7378. This section is the map to it, not a replacement for it.

### The census

Counts below come from walking every variant in `TEXTURE_TREE.json`. The albedo column resolves `_BaseColorMap` first and falls back to `_MainTex`, the normals column counts `_NormalMap` and `_BumpMap`, and the masks column counts `_MaskMap` only. That last point matters: AutodeskInteractive materials carry no `_MaskMap` at all, so a low mask count on a category can mean the category is not on the HDRP mask convention rather than that it shares masks.

| Category | Items | Materials | Distinct albedos | Normals | Masks | Dominant shader |
|---|---:|---:|---:|---:|---:|---|
| Pouches & admin | 129 | 258 | 229 | 39 | 28 | MilkShaders 140 / Lit 111 |
| Plate carriers & vests | 69 | 134 | 117 | 22 | 9 | Lit 90 |
| Tops & jackets | 55 | 72 | 55 | 10 | 8 | ClothWind 71 |
| Other | 51 | 88 | 66 | 24 | 18 | Lit 59 |
| Helmets & head | 32 | 55 | 52 | 12 | 9 | Lit 55 |
| Helmet accessories | 32 | 36 | 29 | 6 | 4 | Lit 36 |
| Ear protection & comms | 29 | 41 | 32 | 17 | 12 | Lit 29 |
| Chest rigs & placards | 27 | 44 | 42 | 8 | 5 | MilkShaders 27 |
| Patches & insignia | 27 | 31 | 26 | 12 | 8 | Lit 22 |
| Belts & holsters | 26 | 50 | 42 | 10 | 11 | Lit 37 |
| Pants & legwear | 24 | 32 | 21 | 5 | 7 | ClothWind 27 |
| Shared camo panels | 22 | 37 | 30 | 6 | 4 | MilkShaders 23 |
| Eyewear | 19 | 22 | 11 | 10 | 9 | Lit 20 |
| Gloves | 19 | 34 | 20 | 14 | 11 | Lit 33 |
| Face & mask | 17 | 19 | 17 | 6 | 5 | Lit 16 |
| NVG | 11 | 16 | 14 | 5 | 5 | MilkShaders 12 |
| Backpacks | 7 | 40 | 40 | 5 | 2 | AutodeskInteractive 32 |
| Knives & tools | 6 | 7 | 5 | 4 | 3 | Lit 5 |
| Climbing & breaching | 6 | 6 | 4 | 4 | 4 | Lit 3 |
| Watches & wrist | 5 | 8 | 5 | 5 | 5 | Lit 7 |
| Body / base mesh | 4 | 4 | 4 | 4 | 3 | Lit 3 |
| Tattoos & body | 2 | 2 | 2 | 0 | 0 | Lit 1 |

Read the last three numeric columns as fan-out, with the mask caveat above. Pouches are 258 materials over 39 normals, so the branch is wide but shallow per family, and one normal edit repaints a whole product line. Backpacks look like extreme fan-out at 40 materials over 5 normals and 2 masks, but 32 of the 40 are AutodeskInteractive and carry roughness and metalness as separate textures, so the two masks only cover the 8 MilkShaders materials in the category.

### Reading a leaf

A leaf in `TEXTURE_TREE.txt` is item, then colourway, then the bound maps with resolution and format:

```
  Ferro_MiniDangler_AOR1   = Ferro Concepts Mini Dangler pouch
     - AOR1             MI_Ferro_MiniDangler_AOR1
         slug   mi_ferro_minidangler_aor1
         shader MilkShaders/Lit-Template
         albedo   Pouch_Ferro_MiniDangler_AOR1_BaseColor  [2048x2048 DXT1]
         mask     MaskMap  [512x512 DXT5]
         normal   Pouch_Ferro_MiniDangler_OCP_DirectX_Normal  [1024x1024 DXT5]
```

The `slug` is the part of the bundle filename between `charactermodmaterials_assets_` and the 32 hex content hash. It is the most stable key you have, with two caveats. In the three multi-material bundles above you also need the material name or PathID to disambiguate. And the colour token inside a slug is not reliable: `MI_D3CRX_V1_BLK` ships in the bundle whose slug is `mi_d3crx_v1_od2`, and `MI_D3CRX_V2_AOR1` ships in `mi_d3crx_v2_black3`.

Item names in the tree are derived from the material name stem, so one real product can split across several tree items. The Spiritus LV-119 appears as six items (`Vest_LV119`, `Vest_LV119_AOR1`, `_BLK`, `_MC`, `_MCB`, `_RG`) because the colourway is baked into each material name rather than carried as a variant suffix.

Across the branch, 952 distinct material names cover 1,036 materials, and 61 names appear in more than one bundle covering 145 bundles. 835 distinct albedo textures cover the 1,000 materials that bind one, and 96 of those textures are bound by more than one material, covering 261 materials. The remaining 36 materials bind no albedo at all.

To pull one family out of the tree:

```sh
grep -n -A6 '^  Pouch_Ferro_GP_6x9' TEXTURE_TREE.txt
python -c "import json;d=json.load(open('TEXTURE_TREE.json'));print(d['character']['Backpacks'].keys())"
```

### Seven shaders, not one

The character branch is not uniformly HDRP/Lit. Each shader name maps to exactly one stripped stub PathID, and no PathID in the whole scan maps to two different shader names, so the PathID is a reliable fingerprint.

| Shader | Stub m_PathID | Materials | Albedo property |
|---|---|---:|---|
| `HDRP/Lit` | `7322088784485553867` | 560 | `_BaseColorMap` on 533; the same 533 also bind `_MainTex`, and in every case the two point at the same texture |
| `MilkShaders/Lit-Template` | `-3661740934848837633` | 286 | `_BaseColorMap`, all 286 |
| `Shader Graphs/ClothWind` | `6692683703723378954` | 109 | `_BaseColorMap` on 101; 10 also bind `_OpacityMap` |
| `HDRP/Autodesk Interactive/AutodeskInteractive` | `-7803075278752278772` | 75 | `_MainTex` on 74 of 75 |
| `HDRP/Hair` | `-8603950788525925452` | 4 | `_BaseColorMap` |
| `GoreShader` | `-1911648095471704572` | 1 | `_BaseColorMap`, plus `_MeatBaseMap` and `_MeatNormalMap` |
| `TextMeshPro/Distance Field` | `683148379888368288` | 1 | `_MainTex` |

`MilkShaders/Lit-Template` is the developer's own shader and binds `_BaseColorMap`, `_MaskMap` and `_NormalMap`, nothing else, on all 286 materials. The 75 AutodeskInteractive materials use the legacy Standard set, `_MainTex`, `_BumpMap`, `_MetallicGlossMap` and `_SpecGlossMap`, with roughness and metalness as separate textures rather than packed into a mask. All 75 bind the bump, metallic and spec maps. A batch tool that writes `_BaseColorMap` across the branch does nothing to any of them, and they are not an edge case:

```
AutodeskInteractive, all 75
  Backpacks ............... MI_Backpack_SATL x16 · MI_Backpack_Minimap x16      32
  Other ................... MI_Bakcpack_Delta x16 · MI_Boots x4                 20
  Plate carriers & vests .. MI_AVS_Zipon_bag x16                                16
  Pouches & admin ......... MI_Puch_Spud_OD x4 · MI_Roll1 x2                     6
  Ear protection & comms .. MI_MPU5                                              1
```

Map formats across the branch, counted over every material that binds the slot: albedos are 952 DXT1, 47 DXT5 and one Alpha8, with 764 at 2048x2048. Masks are 811 DXT5, 76 BC7 and 18 DXT1. Normals are 942 DXT5, 85 BC7 and 4 DXT1. If you restrict the count to the HDRP property names only and drop AutodeskInteractive, the albedo figure becomes 878 DXT1 with 694 at 2048x2048 and the normal figure becomes 867 DXT5, which is the number you will get from a naive `_BaseColorMap`/`_NormalMap` sweep.

The 18 DXT1 mask bindings resolve to 14 distinct textures. DXT1 carries no alpha channel, and HDRP packs smoothness into mask alpha, so the smoothness input on those materials has no data behind it; what the sampler returns in its place is unverified and needs a runtime probe. The 14:

```
MI_BTV_1_MaskMap        MI_Ferro_Dangler_MC_MaskMap   MI_Miscellaneous_02_MaskMap
MI_PMAG_556_MaskMap     MI_Radio_USVR_MaskMap         MI_TEAR_FF_MC_MaskMap
M_Eye_MaskMap           T_Arms_ORM 1_ConvertedMask    pgalsses_low_lenses_MaskMap
T_Glove2_ORM            T_Glove3_ORM                  T_Glove5_ORM
T_Glove6_ORM            T_Glove7_ORM
```

One more branch-wide fact worth building tooling on: of the 3,636 character texture bindings that reference a texture, 3,635 carry scale (1,1) and offset (0,0). The sole exception is `M_Arms` binding `_DetailMap` at scale (2,2).

### Pouches and admin: 129 items, two naming families

The largest branch, and it is two pipelines stacked. The split is by material name shape and shader, and the reading that the brand-named set is the newer one is inference from the naming and the shader choice, not from any build metadata. Family one is unprefixed, one material per colour token, drawn from a small colour vocabulary of `AOR1 AOR2 BLK GRAY M81 MCB OD`. Family two is `MI_Pouch_<Brand>_<Model>_<CW>`, brand-named, on MilkShaders, almost always a three-colour set. A third group sits between them: `MI_`-prefixed but not brand-keyed.

```
Pouches & admin   [129 items, 258 materials, 229 albedos, 39 normals]
│
├─ UNPREFIXED  colour-token materials                             116 materials
│   ├─ Pistol ............ 14  (+ Pistol Double 4)
│   ├─ Pouch ............. 14      ├─ Admin ............... 13
│   ├─ ADA IFAK .......... 9       ├─ Frag ................  7
│   ├─ IFAK .............. 7       ├─ BangerPocket ........  6
│   ├─ Dangler ...........  6  Ferro Concepts
│   ├─ MiniDangler .......  6      ├─ ROLL1 ...............  6
│   ├─ TenSpeed ..........  6  Blue Force Gear Ten-Speed
│   ├─ THOR_Modular_MagPouch 6  NFM THOR, numeric suffixes, no colour token
│   ├─ Bottlepouch .......  4
│   └─ GP_v1_sdr 2 · Puch_Spud_sdr 2 · Roll1_sdr 2 · dope 1 · M4Pmag_sdr 1
│
├─ MI_ PREFIXED, not brand-keyed                                   81 materials
│   ├─ MI_IFAK_V2 ........ 10      ├─ MI_SixPouches ....... 8
│   ├─ MI_DumpPouch ......  7      ├─ MI_IFAK_V3 .......... 7
│   ├─ MI_PistolDouble ...  7      ├─ MI_Puch_Spud ........ 7
│   ├─ MI_Roll1 ..........  6      ├─ MI_Pouch_Spud ....... 6
│   ├─ MI_Ferro_Dangler ..  4      ├─ MI_Pouch_M60 ........ 4
│   ├─ MI_Pouch_40mm .....  4      ├─ MI_Ferro_MiniDangler  3
│   └─ M_Unloading* NPC placeholder art, 32x32 albedos
│
└─ MI_Pouch_<Brand>_<Model>_<CW>                                   61 materials
          shader MilkShaders/Lit-Template · albedo 2048 DXT1 · mask 512 DXT5
          normal 1024 DXT5 named ..._OCP_DirectX_Normal or ..._OCP_OpenGL_Normal
    ├─ Ferro Concepts .. 21: Admin · BangerPocket · Dangler · GP_12x5
    │                        GP_6x5 · GP_6x9 · ROLL1     (3 colourways each)
    ├─ Haley Strategic .. 7: MI_Pouch_Haley_556 set
    ├─ ADA ............. Shingle_Single_M4   {Coyote, OCP, OCP_01}       3
    ├─ Agilite ......... Admin_2ndLayer      {Coyote, OCP}
    ├─ BFG ............. TenSpeed            {Coyote, OCP}
    ├─ Crye Precision .. Frag                {Coyote, OCP}
    ├─ GBRS ............ Pistol_Double       {Coyote, OCP}
    ├─ PLATATAC ........ IFAK_ASAD           {Coyote, OCP}
    │                    material and slug both read PLATATACT
    ├─ RTG ............. Dump_RollUp         {Coyote, OCP}
    ├─ S&S Precision ... M4_Triple_RAP       {OCP, TAN}
    └─ Pincer .......... PistolDouble        {Coyote, OCP}
```

Brand-set normal maps are authored once per product and copied into every colourway bundle, keeping the name of whichever colourway was authored first. `MI_Ferro_MiniDangler_AOR1` binds `Pouch_Ferro_MiniDangler_OCP_DirectX_Normal`; `MI_Pouch_Ferro_Dangler_Coyote` binds `Pouch_Ferro_Dangler_OCP_OpenGL_Normal`. Both conventions sit side by side inside the same brand, and they do not track product type. Within `MI_Pouch_Ferro_*` alone, Admin, BangerPocket, Dangler and GP_12x5 name OpenGL while ROLL1, GP_6x5 and GP_6x9 name DirectX. If you replace a normal, match the convention named in the file. The name records the authoring convention rather than a measured channel, so verify before flipping green.

Shipped misspellings that break naive globs in this branch: `puch_spud*` for "pouch" (nine bundles), `MI_Roll1_coyoe` for "coyote", and `PLATATACT` for PLATATAC in both the material name and the slug.

### Load-bearing: carriers, rigs, placards, belts, backpacks

```
Plate carriers & vests   [69 items, 134 materials]
├─ Crye AVS ......... MI_AVS_{AOR1 ATACS BLK Coyote MC MCB RG}          7
│    all seven albedos 1024, shared AVS_Mask + AVS_MC_Normal
│    AVS_Zipon_bag ... 16 colourways, AutodeskInteractive
│      _MainTex per colourway; _BumpMap/_MetallicGlossMap/_SpecGlossMap shared
├─ Ferro Concepts FCPC V3 .. Vest_Ferro_FCPC_{AOR1, Coyote, OCP}        3
│    albedo Vest_Ferro_FCPC_<CW>_BaseColor 2048 DXT1
│    normal Vest_Ferro_FCPC_OCP_DirectX_Normal 1024 DXT5  (shared by all 3)
├─ Crye JPC ......... JPC_sdr {base,1,2,3} + AOR1 {base,1} + M81 MCB RG
│                     + VET {MC, MCB}                                  12
│    all 12 share "jpc v2 mask" and JPC_MC_Normal 1024
├─ TYR PICO-1M Assaulter's PC .. MI_Vest_TYR_PICO_AssaultersPC{, Coyote} 2
│    all three maps 2048x2048
├─ S&S Precision PlateFrame .... MI_Vest_SNSP_PlateFrame_{OCP, TAN}      2
├─ LBT 6094 ......... MI_Vest_LBT_MC {base,1,2,3}                        4
│    all four named _MC; albedos are AOR1 / AOR2 / MC / none
├─ Spiritus LV-119 .. MI_Vest_LV119_{Coyote AOR1 BLK MC MCB RG}          6
│    LV119_OpenGL_Normal 1024 DXT5 + a 512 DXT5 mask named LitMask
├─ Spiritus CCS / JSTA pouches .. 6 each, CCS at 2048, JSTA at 1024     12
├─ AVS pouches ...... MI_Pouch_AVS x12, albedos 1024 except the 3 VET at 2048
├─ NFM THOR ......... THOR_Vest {base,1,2,3,4,5}                         6
├─ MSV Gen2 ......... MI_Vest_MSV_{BLK, MC}    BLK albedo is 1024, MC 2048
├─ Ferro Slickster .. MI_Ferro_Slickster_MC                              1
├─ BTV .............. MI_BTV_1 x2, one HDRP/Lit and one MilkShaders
├─ Spiritus back panel .. MI_BackPanel_Spiritus 18 + 2 loose colour materials
│    6 more BackPanel items sit in Shared camo panels
└─ Vest <CW> ........ 20 legacy materials, see the parallel-set trap below

Chest rigs & placards   [27 items, 44 materials]
├─ D3CRX chest rig .. V1 {AOR1 BLK OD OD1}  V2 {AOR1 BLK Black.001 MC x2 OD}
│    textures read Rig_Haley_D3CRX_*, so the art says Haley Strategic;
│    classified.json labels it Spiritus Systems, which the art contradicts
│    V1 mask MI_D3CRX_V1_OD_MaskMap_AutodeskToHDRP 2048 BC7
│    two outliers at 1024 albedo / 512 LitMask / 512 normal:
│      MI_D3CRX_V2_BLK (mi_d3crx_v2_blk) and MI_D3CRX_V2_MC (mi_d3crx_v2_mc)
│    slug colour tokens lie: mi_d3crx_v1_od2 holds the BLK material
├─ Spiritus Micro Fight Shingle .. Shingle {AOR1 AOR2 BLK GRAY M81 MCB
│                                  OD Green}                               7
│    albedo of the AOR2 leaf is named "Shigne AOR2", a typo
├─ Spiritus Placard 556 .. {Coyote 1, AOR1 BLK MC MCB RG}                  6
├─ Ferro tear-away ... TEAR {AOR2 BLK GRAY M81 MCB OD} + TEAR_FF {Coyote,
│                      AOR1, MC, OCP}                                     10
│    TEAR_FF_MC is the one HDRP/Lit leaf in the set; the other three
│    are MilkShaders
├─ Ferro KTAR-FF placard .. {Coyote, AOR1, OCP}                            3
├─ Agilite Pincer triple placard .. {Coyote, OCP}  masks and normals 2048    2
└─ Triple .............. {AOR1 AOR2 BLK GRAY M81 ODG}                      6

Belts & holsters   [26 items, 50 materials]
├─ Battle Belt ..... Belt {AOR1 AOR2 BLK GRAY M81 MCB OD Green}            7
│    all seven bind Belt_TYR_Gunfighter_MAB_OCP_DirectX_Normal, so this
│    legacy set is the TYR Gunfighter MAB belt under a generic name
├─ Bison belt ...... MI_Belt_Bison_{Coyote AOR1 AOR2 MC MC1 MCB RG}        7
│    all three maps 2048x2048; ~13.3 MB of texture payload per bundle,
│    the heaviest belt in the branch
├─ TYR Gunfighter .. MI_Belt_TYR {base,1} + {AOR1 AOR2 Coyote MCB RG}      7
│    maps are 1024 except the AOR1 albedo, which ships 2048
│    + MI_Belt_TYR_Gunfighter_MAB {Coyote x2, OCP}                         3
├─ Crye Abdominal Pro .. MI_AmbdominalProCrye x4 + AmbdominalPro_sdr x2     6
│    name is misspelled "Ambdominal"; three of the four Crye ones
│    bind no albedo
├─ Safariland 6354DO .. MI_Holster_63540DO {base,1,2,AOR1,AOR2,RG}
│    + _Strap {base,1,RG} + MI_Holster_Safariland_63540DO {Coyote,OCP,Strap}
│                                                                         12
│    "63540DO" is a digit transposition of 6354DO
├─ Safariland SHR/QLS .. MI_safariland_shr {1 x2, AOR1 AOR2 Coyote OD}      6
└─ G17 ............. holstered pistol body, 2 materials

Backpacks   [7 items, 40 materials]  mixed pipelines, check the shader first
├─ Mystery Ranch SATL .. 16 colourways, AutodeskInteractive
│    _MainTex  per colourway, 2048 DXT1
│    _BumpMap  SATL_Black_Pack_Normal      2048 DXT5  ┐
│    _MetallicGlossMap SATL_Black_Pack_Metalness 2048 ├ shared by all 16
│    _SpecGlossMap     SATL_Black_Pack_Roughness 2048 ┘
├─ AVS Minimap pack .... 16 colourways, AutodeskInteractive
│    _SpecGlossMap shared by all 16; _BumpMap and _MetallicGlossMap split
│    1 Black / 15 MC
├─ Arc'teryx LEAF ...... MI_Backpack_Arcteryx_LEAF_{Gray, OCP}  MilkShaders
└─ Ferro KTAR .......... 6 colourways {AOR2 BLK GRAY M81 MCB OD} MilkShaders

The "Delta" bag is filed under Other, not here: 17 materials, 16 in
mi_bakcpack_delta* bundles plus one in the `delta` bundle. MI_Bakcpack_Delta
exists twice, once on HDRP/Lit with _BaseColorMap in `delta` and once on
AutodeskInteractive with _MainTex in `mi_bakcpack_delta`.
```

### The parallel-colourway trap

The legacy unprefixed sets are not one set. `Vest <CW>` is **three parallel colourway sets, one per carrier platform**, all using the same material name and the same albedo texture name, separated only by a trailing digit on the bundle slug. The platform is identifiable only by which normal map the material binds:

```
material       slug          normal map bound                              platform
Vest AOR1      vestaor1      Vest_TYR_PICO_AssaultersPC_OCP_DirectX_Normal TYR PICO
Vest AOR1      vestaor11     Vest_SNSP_PlateFrame_OCP_DirectX_Normal       S&S
Vest AOR2      vestaor2      Vest_TYR_PICO_AssaultersPC_OCP_DirectX_Normal TYR PICO
Vest AOR2      vestaor21     Vest_Ferro_FCPC_OCP_DirectX_Normal            Ferro FCPC
Vest AOR2      vestaor22     Vest_SNSP_PlateFrame_OCP_DirectX_Normal       S&S
Vest BLK       vestblk       Vest_Ferro_FCPC_OCP_DirectX_Normal            Ferro FCPC
Vest BLK       vestblk1      Vest_SNSP_PlateFrame_OCP_DirectX_Normal       S&S
Vest BLK       vestblk2      Vest_TYR_PICO_AssaultersPC_OCP_DirectX_Normal TYR PICO
Vest GRAY      vestgray      Vest_Ferro_FCPC_OCP_DirectX_Normal            Ferro FCPC
Vest M81       vestm81       Vest_TYR_PICO_AssaultersPC_OCP_DirectX_Normal TYR PICO
Vest MCB       vestmcb1      Vest_Ferro_FCPC_OCP_DirectX_Normal            Ferro FCPC
```

Nothing about the suffix ordering is consistent. `vestaor21` is FCPC while `vestblk1` is S&S, and the undigited slug is TYR PICO for AOR1, AOR2, M81 and MCB but FCPC for BLK, GRAY and OD. Three colours also break the digit scheme entirely and use distinct names instead: `vestod` (FCPC), `vestodgreen` (S&S), `vestodg` (TYR PICO). Twenty materials in total.

The same structure repeats on `Pouch <CW>` (14 materials, split between RTG Dump RollUp and S&S M4 Triple RAP), `Pistol <CW>` (14, split between Agilite Pincer and PistolDouble) and `Admin <CW>` (13, split between Agilite Admin 2nd Layer and Ferro Admin, where the two platforms also differ by normal convention, DirectX for Agilite and OpenGL for Ferro). This is where the surplus prefab colourways that have no `MI_`-prefixed material resolve. Key every mod entry on the bundle slug, and confirm the platform by reading the bound normal, never by the material name.

### Head: helmets, covers, ear pro, NVG

```
Helmets & head   [32 items, 55 materials]     all 55 on HDRP/Lit
├─ Crye AirFrame ... MI_Airframe_Helmet {base AOR1 AOR2 Coyote MC MCAlpine
│                    MCJungle RG Tan} + " 1" {BLK MCAlpine1 MCB1 RG1 Tan1}
│                    + " 2" {BLK, MCB} + " Worn" {Coyote}          17 materials
│    every one binds Airframe_Helmet_Normal 2048 DXT5 and LitMask 2048 DXT5
│    albedos are 2048 DXT5 named for the camo alone: "AOR1",
│    "Multicam Alpine", "Tan Battleworn", "Ranger Green Battleworn"
│    one name lies: MI_Airframe_Helmet Multicam Alpine 1 binds a texture
│    called "Multicam Arid"
├─ Ops-Core FAST .. MI_Opscore_FAST_MT_Helmet_{BLK2 CoyoteBrown FDE FDE1
│                   FDE2 FDErt RG Tan WORN}                         9 materials
│    the prefabs are FAST XP, the materials are all named FAST_MT.
│    No material anywhere in the scan contains "fast_xp", though 8 textures do.
│    the FDE names lie: _FDE binds Opscore_FAST_XP_MC_BaseColor,
│    _FDE 1 binds the Black basecolor, only _FDE 2 binds the FDE one
├─ Super High Cut .. MI_SuperHighCut {base,1,black} + _Ac + _Ac 1
│                    + _AC_blk + _MC                                       7
│    two more (_AC_blk flag, _MC USFLAG) are filed under Patches
├─ MICH Low Cut .... MI_MICH_LC_AC + MI_MICH_LC_Painted {base,1,2,3}       5
│    whether the BLK / FDE / OD prefabs have a material bundle of their own
│    is unverified from the scan, needs a prefab probe
├─ IHPS ............ MI_Helmet_IHPS_{BLK, TAN}, slugs mi_helmet_ihps_*      2
│    both bind SM_Helmet_IHPS_Coyote_Normal
├─ Team Wendy EXFIL. MI_TeamWendy_Exfil {base, 1}                          2
├─ ATE HHV ......... MI_ATE_HHV_Helmet                                     1
├─ Boonie .......... {BLK Coyote M81 MC TIGSTRIPE OD GRN}                  6
│    slug trap: "Boonie MC" is booniem811, "Boonie TIGSTRIPE" is
│    booniecoyote1, "Boonie OD GRN" is booniem812
└─ Beanie .......... {RG black gray multicam tan white}                    6

Helmet accessories   [32 items, 36 materials]  all HDRP/Lit, 6 normals, 4 masks
├─ Ops-Core FAST cover .. 18 materials
│    _AMCU _AOR1 _AOR2 _BLK _CoyoteBrown _M81 _MC _MC1 _MC2 _MCAlpine
│    _MCArid _MCB _MCJungle _MTP _RG _TAN _TS _TSD
│    all 18 share Opscore_FAST_XP_Cover_Normal 2048 and LitMask 2048
├─ Crye AirFrame cover .. MI_Airframe_Cover {base,1,2,3,4,6,7,8,9,10}     10
│    there is no "Cover 5"; the numbering skips it
├─ IHPS cover ........... MI_Helmet_IHPS_Cover {BLK, MC, TAN}              3
├─ Wilcox G24 mount ..... MI_Mount_G24_Wilcox_Tan {base, 1}                2
│    the claim that the prefabs call it L24 is unverified from the scan,
│    no material or texture anywhere contains "l24"
├─ Kagwerks S7 shroud ... MI_KAGWEKS_S7   material and slug drop the R;
│                         the textures keep it (KAGWERKS_S7_BaseColor)     1
└─ tape ................. tape, tape 1                                     2

Ear protection & comms   [29 items, 41 materials]
├─ Peltor ComTac 6 ... MI_Comtac_6 {x2} + MI_Comtac_6 MC {x5}              7
│    the 5 MC leaves are identical bindings, one per wrap prefab pair
├─ Peltor ComTac 4 ... Comtac_4_Headset, 2048 BC7 mask and normal          1
├─ Peltor ComTac VI .. MI_EarPro_Comtac_VI_DE  binds Comtac_6_BaseColor    1
├─ Peltor "Comntacts" helmet set .. MI_Comntacts + comntacts_sdr           2
├─ Ops-Core AMP ...... MI_AMP_Tan (1024) + MI_AMP_blk (2048)               2
│    blk is not a tint: its _BaseColor is 1,1,1,1 and it binds a separate
│    2048 rebake of the Tan basemap
├─ MSA Sordin ........ MSA1                                                1
├─ Walker's Razor .... MI_Walkers_Razor                                    1
├─ Persistent MPU5 ... MI_MPU5 {base, BLK, Coyote, MC} + MPU5_sdr {base, 1,
│                      blk ataks, blk radio, blk vet}                      9
│    MI_MPU5 is the one AutodeskInteractive material outside packs and boots
├─ Radio <CW> ........ legacy set {AOR2 BLK GRAY M81 MCB OD}, all binding
│                      Pouch_Ferro_Radio_Pocket_OCP_DirectX_Normal          6
├─ Ferro radio pocket  MI_Pouch_Ferro_Radio_Pocket {Coyote, AOR1, OCP}     3
└─ TrellisWare TW-950 1 · Baofeng UV-5R 1 · TEA TBAS V5 2 · HX2 3 · USVR 1
   AN/PRC-152 lives in Other

NVG   [11 items, 16 materials]
├─ NVG <CW> ......... 6 materials {AOR2 BLK GRAY M81 MCB OD}
│    these bind Pouch_Ferro_NVG_OCP_OpenGL_Normal, so they are the legacy
│    colourway set of the CHEST POUCH, not of any device
├─ GPNVG-18 ......... MI_GPNVG18_Tan {base, 1, Lens O, Lens O GP}          4
├─ L3Harris F-PANO .. MI_NVG_L3Harris_FPGPNVG_FDE + _Mount_FPG_21_FDE      2
├─ Safran ECOTI ..... MI_Safran_ECOTI, one material. Whether it serves all
│                     nine "withecoti" mounts is unverified, needs a probe
├─ PVS-14 / PVS-31S . pvs14_sdr · pvs31_sdr   (filed under Other)
└─ Ferro NVG pouch .. MI_Pouch_Ferro_NVG {AOR1, AOR1 1, OCP}               3
     the chest pouch, not a device; editing it changes no helmet
```

### Worn cloth: tops, pants, gloves, face

Clothing is the ClothWind branch: 71 of the 72 Tops & jackets materials and 27 of the 32 Pants & legwear materials run `Shader Graphs/ClothWind`.

```
Tops & jackets   [55 items, 72 materials, 55 albedos, 10 normals]
├─ Crye G3 combat shirt LS .. MI_CRYE_LS {base,1} + MCA MCB Flecktarn Khaki
│                             Marpat {base,1} Tropic AOR1WORN DTS Gray M81
│                             MCBWORN MCWORN RG                          16
│    all 16 share crye_g3_3 1024 as the mask
│    13 bind CRYE_G3_MC_Normal_4, at 1024 except in the DTS and M81
│    bundles where the same name ships at 2048; the three WORN leaves bind
│    cryeg2g3_MI_CRYE_LS_Normal at 4096 instead
├─ Crye G3 "cut" ............ crye g3 {MC MCB} · crye g3 aor 1..8
│                             (aor 3 and aor 4 exist twice)
│                             crye g3 cut aor 4..8 · crye g3 cut MCB     18
│    there is no material named "crye g3 cut"
│    the "aor" numbering lies: aor 5/6 bind TigerStripe albedos,
│    aor 7/8 bind MC Arid. The trailing digit on the TEXTURE name tracks
│    the garment slot (_1/_4 top, _9/_12 pants); that pairing holds across
│    every leaf in the set, which is inference from the pattern, not a
│    documented convention
├─ Patagonia jacket ......... MI_Jacket_Patagonia {base, 1..10}          11
│    all 11 share PatagoniaMask + Patagonia_Black_Normal, all albedos 2048
├─ Patagonia flannel ........ PataFlannel {BLK, RED}                      2
│    same mask, different normal (patago_MI_Jacket_Patagonia_Normal)
├─ PCU Level 7 .............. MI_PCU {Coyote, Tigerstripe, FDE, OD}
│                             + _Grey + _Grey 1 x2 + _Grey 2 x2           9
│    MI_PCU_Grey and one MI_PCU_Grey 1 ship 4096x4096 albedos
│    four materials (Coyote, FDE, OD and the mi_pcu_grey11 copy of
│    MI_PCU_Grey 1) all bind PCU_Jacket_HoodDown_MI_PCU_Grey_BaseMap,
│    so they render alike
├─ Rugby shirt .............. MI_Rugby_Shirt_COYOTE + _FDE + _FDE 1..6   10
├─ T-shirt .................. MI_TShirt_3DMA_Snake {base,1,2,3}
│                             + MI_TShirt_USA                             5
│    the unnumbered MI_TShirt_3DMA_Snake binds no albedo but carries
│    _BaseColor 0.7547,0.7547,0.7547, so it renders as a plain grey shirt
└─ M_ShirtAOR2 .............. the one NPC leaf in this category           1

Pants & legwear   [24 items, 32 materials, 21 albedos, 5 normals]
├─ Crye G3 pants .... MI_CRYE_Pants Flecktarn Khaki Marpat {base,1} Tropic
│                     AOR1WORN DTS Gray M81 MC {base,1} MCA MCB MCBWORN
│                     MCWORN RG                                          16
│    all 16 share CryePantMask 1024
│    12 bind CRYE_G3_MC_Normal_12 at 1024; the DTS bundle ships that same
│    name at 2048; AOR1WORN, MCBWORN and MCWORN bind
│    cryeg2g3_MI_CRYE_Pants_MC_Normal at 4096
│    MI_CRYE_Pants_DTS and _AOR1WORN ship a 1024x1024 single-mip albedo
│    where the other 14 ship 2048x2048 with 12 mips
├─ Levi's jeans ..... MI_Jeans_LEVIS {base x2, 1 x3, 2 x2, 3, Gray}       9
│    all nine share jeans_mask; two normals, not one
└─ NPC M_ pants ..... PantsCamoD(+_nowind) · PantsCamoI
                      PantsColorA(+_nowind) · PantsColorC · PantsColorE   7
     per-variant masks, so a new camo here costs two textures, not one

Shared camo panels   [22 items, 37 materials]
├─ Ferro GP panel sizes .. 12x5 · 6x5 · 6x9, each {AOR2 BLK GRAY M81 MCB OD} 18
│    6x5 and 6x9 bind Pouch_Ferro_GP_<size>_OCP_DirectX_Normal 1024 DXT5
│    12x5 binds Pouch_Ferro_GP_12x5_OCP_OpenGL_Normal 1024 DXT5
│    the same split holds on the MI_Pouch_Ferro_GP_* set in Pouches & admin
├─ Spiritus back panel ... BackPanel_AOR1 · BackPanel_Coyote_BaseColor
│                          · BackPanel_M81_BaseColor 1 · _MC · _RGreen
│                          · _Tigerstripe 1                                6
│    all six share BackPanel_Spiritus_MC_Normal + LitMask
├─ Camo tokens ........... AOR1 · AOR2 · M81 · BLK GRAY · BLK OD GREEN     5
│    despite the names, all five bind Safariland 63540DO holster art and
│    Holster_Safariland_63540DO_OCP_DirectX_Normal
├─ red 1 ................. binds the TShirt_3DMA_Snake basemap            1
└─ TINT-ONLY, no albedo bound at all, colour lives in _BaseColor (ClothWind)
     Blue  0.1752, 0.2514, 0.2925      Brown 0.3098, 0.2542, 0.1529
     FDE   0.3585, 0.3174, 0.2587      Gray  0.2547, 0.2547, 0.2547
     Greem 0.1686, 0.3098, 0.1529 [sic] RG    0.2961, 0.3113, 0.2717
     red   0.2925, 0.1586, 0.1586
     all seven share tShirtMask + TShirt_Black_Normal. Adding a solid shirt
     colour here is a two-number edit; adding a pattern is not possible
     without wiring a new _BaseColorMap.

Gloves   [19 items, 34 materials, 20 albedos, 14 normals]
├─ Mechanix .. 26 materials: 13 Left, 12 Right, plus
│              MI_MechanicxGloves_Left_Black
│    the name misspells the brand as "Mechanicx"; MechanixWhiteBlack 1
│    spells it correctly. Left and Right are separate materials.
│    MechanicxGloves_*_FDE and *_Coyote Tan bind the same albedo on
│    both hands.
├─ NPC set ... Glove3 · Glove4 · Glove5 · Glove6 · Glove7 · Gloves 2
│    T_Gloves<N>_*_D albedo + T_Glove<N>_ORM packed mask + T_Glove<N>_N
│    Glove4 binds no mask at all
└─ Petzl Cordex folded .. MI_Gloves_PETZL_CORDEX_Folded (MilkShaders)

Face & mask   [17 items, 19 materials]
├─ Balaclava .. Coyote · MC · MCB 1 · Phantom_BLK · Phantom_Tan · RG
│               Skull · Tan · Tan 1 · WH                                 10
│    all ten share MI_Balaclava_WH_MaskMap_AutodeskToHDRP 512 BC7 and
│    Balaclava_Normal 1024 BC7. Coyote and WH both resolve into the
│    balaclava_MI_Balaclava_Tan_BaseMap family and look alike; Tan and
│    Tan 1 bind their own textures.
│    the two Phantom leaves are the only 2048 albedos, the rest are 1024
├─ Ops-Core SOTR .. MI_SOTR                                               1
├─ Beards ......... Beard1 · Beard2 · Beard3 · blackbeard                 4
│    Beard1/2/3 are three of the four HDRP/Hair users in the branch;
│    blackbeard is HDRP/Lit
└─ bandana · face x3                                                      4
```

### Patches, eyewear and the shallow tail

```
Patches & insignia   [27 items, 31 materials]
├─ US flag ..... MI_Patch_US_BW · MI_Patch_US_BW 1 · MI_Patch_US_IR
│                MI_Patch_3DMA_US_CO · MI_Patch_3DMA_US_IR · USA · usa ir
│    among the nine *patch*-slug bundles, MI_Patch_3DMA_US_CO is the only
│    MilkShaders material; the category as a whole has six MilkShaders
│    materials, the other five being the Vet set
│    MI_Patch_US_BW 1 is misnamed: it binds Patch_US_IR_BaseColor and is
│    the only patch with an _EmissiveColorMap, authored with
│    _EmissiveColor 0,0,0,0
├─ Unit / role . MI_Patch_JTAC · Patch Golden SQ · Seal Team
│                MI_Patch_US_Snake_02 · Patch_3DMA_sdr {x2}
├─ Morale ...... Willy · bigmamashouse · docminty · nikkkooooooootirz
│                stupid · sygic · syko · waterboardinginstructor
│                wile e coyote · middle east   (APAmazing and GreyFox are
│                filed under Other)
├─ Helmet flags. MI_SuperHighCut_AC_blk flag · MI_SuperHighCut_MC USFLAG   2
├─ Veteran ..... Vet {Coyote GRAY MCB Multicam OD Green}                   5
│    all five bind vetbp_MI_Bakcpack_Delta_* maps
└─ Text ........ "amarurgt SDF Material", the branch's only
                 TextMeshPro/Distance Field material, on a 1024 Alpha8 atlas

Eyewear   [19 items, 22 materials]  11 albedos for 22 materials
├─ ESS Crossbow .. frame {Black, FDE} + lens {Black, Black Dark, Orange}   5
│    all 1024x1024; MI_ESS_Crossbow_Lens_Orange binds only a normal
├─ Gatorz Magnum . Eyepro_Gatorz {base, 1, 2, _2, Blue, Red, Yellow}
│                  over 7 bundles, gatorz_folded holding two               8
│    six of the eight bind no albedo; Eyepro_Gatorz_2 binds nothing at all
├─ Oakleys ....... MI_Oakleys, the one MilkShaders leaf here              1
├─ aviators · glasses · glasses.002 · lenses · lenses 1 · watchglass x2
└─ Mag_Magpul_PMAG_GEN3_M3_Glass, filed here by the classifier

Knives & tools   [6 items, 7 materials]
  MI_BR_Knive (+_blk, _sdr) · MI_Knife_SWK_Guardian · MI_Multitool
  glowstick x2, one of which sits in the `delta` bundle and binds nothing

Climbing & breaching   [6 items, 6 materials]
  DMM XSRE carabiners on Ferro hardware, all maps 512x512:
    MI_Carabiners_Ferro_DMM_XSRE_03
    MI_FCarabiners_Ferro_DMM_XSRE_01  mask is named "01MaskMap"
    MI_FCarabiners_Ferro_DMM_XSRE_03
  MI_Bolt_Cutters · MI_Heli_Tether · MI_Heli_Tether 1 (binds no albedo)

Watches & wrist   [5 items, 8 materials]
  MI_Watch_Sangin {base, 1} · MI_Mike_Watch 1 · garmin · bracelet x4

Body / base mesh   [4 items, 4 materials]
  M_Head (the one GoreShader material in the branch) · M_Eye · M_Arms · Loins
  M_Arms carries the 4096 albedo and the only non-identity texture
  transform in the branch

Tattoos & body   [2 items, 2 materials]
  M_EyeLashes (HDRP/Lit) and MI_Eyelashes (HDRP/Hair): different prefix,
  different capitalisation, different shader, neither binds a normal

Other   [51 items, 88 materials]  the classifier's overflow, not junk
  AN/PRC-152 radio 19 materials · Bakcpack_Delta 17 · Boots 4
  Magpul PMAG / EMAG / SIG M17 mag / Mag_20R / Bullet_556 (character copies
  of weapon parts; the weapon branch has its own twins)
  Miscellaneous_01 family 13 · Vethat 7 · Miscellaneous_02 x3
  PVS14_sdr · PVS31_sdr · Modlite rifle light · Airboss LBM gas mask
  hands · body x3 · Strap x2 · loose holster colour tokens
```

### Best reskin entry points

Pick by fan-out per texture and by how clean the colourway-to-bundle mapping is.

**Spiritus LV-119** is the cleanest start in the branch. Six materials, six bundles, six albedo textures, one `LV119_OpenGL_Normal` and one 512 mask per bundle. Repaint one 2048x2048 DXT1 albedo and you have a new colourway with no risk to the other five. Two naming traps to watch: four of the six albedos share the stem `SPIRITUS SYSTEMS LV119 Coyote_MI_Vest_LV119_Coyote_BaseMap` and differ only by a Unity duplicate suffix, and the mask is called `LitMask`, a generic name bound by 144 materials in this branch at three different resolutions. Neither name identifies an asset. Go by bundle.

**Helmet accessories** is the highest ratio of visible surface to work: 36 materials over 6 normals and 4 masks, all on HDRP/Lit, and 27 of the 32 items are covers, covering 31 of the 36 materials. The Ops-Core cover family alone is 18 colourways sharing `Opscore_FAST_XP_Cover_Normal`, and the cover is what a player actually sees on a helmet. Adding an eleventh AirFrame cover slots into an existing numbering gap, since `MI_Airframe_Cover 5` does not exist.

**Brand-named pouches** give the widest blast radius per edit. A single bundle such as `mi_pouch_ferro_gp_6x9_coyote` carries one 2048x2048 albedo with its own UV island set, independent of any carrier atlas, and that pouch mesh appears on many carriers. Whether the runtime binds one material per sub-renderer or sprays one material over the whole carrier is unproven and decides the scope of a pouch-level project. Walk `GetComponentsInChildren<MeshRenderer>()` on a spawned carrier and log `renderer.material.name` before committing.

**Crye G3 pants and long-sleeve** are the best clothing donor, with caveats. Sixteen camos each and one shared mask per garment (`CryePantMask` for pants, `crye_g3_3` for the shirt), and every binding in the branch sits at scale (1,1) offset (0,0), so the sampler transform is identical across all thirty-two. That is consistent with a shared UV layout but does not prove one; confirm on the mesh if it matters. The normal is not shared cleanly: twelve pants leaves bind `CRYE_G3_MC_Normal_12` at 1024, the DTS bundle ships that same name at 2048, and the three WORN leaves bind a 4096 normal under a different name, with the same pattern on the shirt. Four leaves ship a broken albedo (`MI_CRYE_LS_M81`, `MI_CRYE_LS_DTS`, `MI_CRYE_Pants_DTS`, `MI_CRYE_Pants_AOR1WORN` are 1024x1024 with one mip), and one ships oversized (`MI_CRYE_LS_AOR1WORN` at 4096). Normalising those five to 2048 with a full mip chain is a quality win with no design decisions attached.

**Patches** are the smallest self-contained surface, though not the whole set. The nine `charactermodmaterials_assets_*patch*` bundles hold the flag and unit patches; the Patreon morale patches live in their own single-material bundles under their own names, so the category is 27 items and 31 materials in total. Six of the nine patch bundles ship an albedo with `m_MipCount = 1`, covering four distinct textures, so generating mip chains there is free work. The claim that the morale patches share one 76-vertex `CustomPatch` mesh whose UV0 occupies the top 30 percent of the texture is not supported by the scan and needs a mesh probe before you paint to it.

Three areas are poor first projects. Backpacks look attractive at 16 colourways with clean 1:1 mapping, but 32 of the 40 materials run AutodeskInteractive with `_MainTex`, `_BumpMap`, `_MetallicGlossMap` and `_SpecGlossMap`, so tooling built for the HDRP property names does nothing there; the exceptions are the Ferro KTAR and Arc'teryx LEAF materials, which are MilkShaders. The legacy `Vest`, `Pouch`, `Pistol` and `Admin` sets are the parallel-set trap above, and picking the wrong trailing digit repaints a platform you did not mean to touch. The NPC `M_` and `Glove2..7` sets carry per-variant masks and packed ORM textures rather than the HDRP mask convention, so a new camo there costs two authored textures instead of one.

Whatever you pick, key your manifest on the bundle slug, not on the material or texture name. 61 material names in this branch belong to more than one bundle, 96 albedo textures are bound by more than one material, generic names like `LitMask` and `MaskMap` cover several distinct assets each, and the colour in a texture's name is frequently the colour of whichever sibling was authored first.

## 6. The weapon attachment tree

The weapon side ships as two Addressables families. `weaponmods_stripped` holds 1,581 prefab bundles, one per attachment, carrying meshes and hierarchy. `weaponmodmaterials` holds 1,203 bundles carrying 1,204 materials; exactly one bundle, `weaponmodmaterials_assets_bolt_b8825acb419053a014c26bef292d85cb.bundle`, holds two (`MI_Body` and `M4`). Resolving those 1,204 materials to the products they paint gives 1,066 items in 19 categories.

### The category census

Item and material counts come from the classified tree; the columns sum to 1,066 and 1,204. The finish column is a token census over material name, bundle slug and every bound texture name, where a token counts only when it is delimited by a non-alphanumeric character, with `OLIVE` folded into `OD`, `GOLDEN` into `GOLD` and `MULTICAM` into `MC`. The tokens are real strings in the shipped names. Reading them as finishes is inference.

| Category | Items | Materials | Finish tokens counted in names |
|---|---:|---:|---|
| Handguards & rails | 165 | 196 | BLK 14, FDE 14, GRY 4, OD 4, GRN 1, MC 1 |
| Receivers & bodies | 145 | 181 | FDE 34, BLK 16, WHITE 6, GRN 4, GRY 4, OD 3, SAND 3 |
| Optics & sights | 120 | 136 | BLK 15, FDE 12, RED 4, TAN 3, GRY 1, MC 1, OD 1 |
| Suppressors & muzzle | 92 | 100 | BLK 23, FDE 11, TAN 7, SAND 3, SILVER 1 |
| Stocks & buffers | 88 | 102 | BLK 9, FDE 8, GRY 5, OD 5, GRN 1, MC 1, SAND 1 |
| Barrels | 84 | 84 | FDE 8, BLK 7, BRONZE 1, CHAMELEON 1, SAND 1, STEEL 1 |
| Magazines | 73 | 80 | BLK 7, FDE 7, SAND 2, MC 1, OD 1, SILVER 1, STEEL 1, WHITE 1 |
| Reticles & scope glass | 71 | 75 | RED 2, BLK 1 |
| Grips & foregrips | 61 | 64 | FDE 7, BLK 6, OD 5, GRN 2, GRY 2, MC 1, SAND 1, TAN 1 |
| Other | 57 | 68 | BLK 14, FDE 5, STEEL 3, GRN 2, GRY 1, MC 1, OD 1, TAN 1, WHITE 1 |
| Mounts & rings | 31 | 35 | FDE 4, BLK 3, OD 1 |
| Iron sights | 20 | 21 | none |
| Lights & lasers | 17 | 19 | FDE 2, BLK 1, GRN 1 |
| Charging handles & controls | 14 | 15 | BLK 1, TAN 1 |
| Shotgun parts | 13 | 13 | none |
| Chassis & stocks (precision) | 6 | 6 | none |
| Ammunition & shells | 4 | 4 | SILVER 1 |
| VFX / distortion | 4 | 4 | none |
| Bipods & supports | 1 | 1 | none |
| **Total** | **1,066** | **1,204** | 294 materials carry at least one token |

Receivers & bodies is the only category with an exotic tail, and the row above is truncated to the top seven tokens. The rest of it is BRONZE 2, CHAMELEON 2, GOLD 2, STAINLESS 2, COYOTE 1, MC 1, MCB 1, RED 1, STEEL 1, TAN 1, almost all of it the L2D and Zaffari Glock slide and barrel ecosystem.

Read the total first. Only 294 of 1,204 weapon materials carry any finish token at all. Do not read that as 910 single-finish parts. Colourway siblings routinely ship with no token anywhere in the material name, the slug or the texture name, which is the subject of the naming-traps section below. The token census tells you where finish names are used, not where finishes exist.

Bipods & supports is the floor of the whole game, one item and one material, `SPR300_Bipod`. The other bipod material in the game, `Bipod_Cvlife-6-9Inches-MLOK`, is filed under Handguards & rails because it is an M-LOK mount. Neither has a colourway sibling, and the deployed and folded prefabs (`spr300bipoddeployed`, `spr300bipodundeployed`, `bipoddeployed`, `bipodfolded`) are state, not finish.

Bare numeric item names are barrel lengths in inches. Thirteen items in the tree have a numeric name and twelve of them sit in Barrels: `9_5`, `10_5`, `12_5` and `13`, each appearing three times because Addressables appends a Unity duplicate-name suffix. The three copies are three finishes, and you can only tell them apart by opening them. For the 10.5 inch barrel, slug `10_5` binds `10_5_BaseColor_BLK`, `10_51` binds `10_5_BaseColor_DRK_FDE` and `10_52` binds `10_5_BaseColor_FDE`, all 2048x2048 DXT1 on HDRP/Lit. The thirteenth numeric item is `1`, in Other, classified as a placeholder.

### Where the colour lives

Not in the prefab. Of the 1,581 `weaponmods_stripped` bundles, 1,518 contain no `Material` object at all, and every `MeshRenderer` in the ones sampled points `m_Materials[0]` at the same external sentinel, `m_FileID=2 / m_PathID=-5343436609273763532`. Cloning a prefab to make a new colourway gets you nothing.

The 63 prefab bundles that do carry inline materials hold 206 materials between them, and they are worth knowing because the blanket rule "all finish work happens in weaponmodmaterials" has exceptions. Most of the 206 are optical (`Scope Lens`, `Scope NVG Filter`, `Eye Relief Visualization`, the EOTech reticle and glass, the ACOG render target and ARD) or ammunition (`277_Bullet.001`, `762x54_Case.001`, `Primer_Used_Gold`). Four `HDRP/Decal` materials named `Decal` and `Decal 1` ship two apiece in the two Daniel Defense MK18 upper bundles. Twelve bundles hold a body material, and seven of the eight distinct names have no counterpart anywhere in `weaponmodmaterials`: `CRC9U028`, `Handguard_RIS-II-9.5`, `PistolGrip_STD`, `HK_HandguardQuad`, `Rotex2 1`, `M600V` and `Riser`. Only `6L31_545x39` exists in both families.

Those inline materials are stubs, not finishes. Open `surefirem600v` against `surefirem600vfde`, and `crc2u073` against `crc2u073fde` and `crc2u073odg`, and hash the decoded pixels: every texture is byte-identical across the siblings, and `_BaseColor` is `0.9063,0.9063,0.9063,1` in all five. The FDE and ODG prefab bundles ship an exact copy of the black one. The real colourways are where they always are. `weaponmodmaterials` ships `crc2u073_opticmount`, `crc2u073_opticmountfde` and `crc2u073_opticmountodg`, three materials binding three different albedos (`CRC2U073_OpticMount_BaseColor`, `... 1`, `... 2`) over a shared normal and ORM pack, all three with `_BaseColor` at pure white. Ignore the inline stub and edit the material bundle.

The M600V is the one case where that does not close cleanly. Its two `weaponmodmaterials` bundles, `m600v` and `m600v1`, bind the *same* albedo `m600v_AlbedoTransparency` and differ only in `_MaskMap` (`m600v_MaskMap` against `m600v 1_MaskMap`). No albedo anywhere in the family expresses an FDE M600V, so where that colour comes from is unresolved and needs a runtime probe. More generally, which material index each prefab selects is not recoverable from the files. The IL2CPP identifier table contains `ActuallySetMaterial`, `ApplyColorModifier` and `materialIndex`, which implies a runtime selection path, but that call chain has not been disassembled.

### Shader families and their property sets

| Shader | Materials | Albedo slot | Mask semantics |
|---|---:|---|---|
| `HDRP/Lit` | 729 | `_BaseColorMap` plus `_MainTex` alias | HDRP MaskMap (R metallic, G AO, B detail, A smoothness) |
| `MilkShaders/UERemap` | 207 | `_BaseColorMap` | `_MaskMap` is an Occlusion/Roughness/Metallic pack |
| `MilkShaders/Lit-Template` | 153 | `_BaseColorMap` | ORM pack |
| `HDRP/Autodesk Interactive/AutodeskInteractive` | 39 | `_MainTex` only | `_MetallicGlossMap`, `_SpecGlossMap`, `_OcclusionMap`, `_BumpMap` |
| `Ultimate Scope Shaders/HolographicSight` | 39 | none | `_Reticle` |
| `Shader Graphs/RealisticScopeEffect` | 18 | none | lens path |
| remainder (KriptoFX FX, `Shader Graphs/Reticle`, `HDRP/Unlit`, IR laser, NVG, `Ultimate Scope Shaders/Scope`) | 19 | varies | varies |

Two consequences. All 39 Autodesk Interactive materials bind `_MainTex` and none binds `_BaseColorMap`, so a reskin tool that writes only that property does nothing to them; they need the legacy Standard-shader slot names. And on HDRP/Lit the `_MainTex` alias is real: 710 weapon HDRP/Lit materials bind `_BaseColorMap`, the same 710 bind `_MainTex`, and all 710 bind the identical texture to both slots, zero exceptions. Write both, or code reading `_MainTex` keeps the old map.

Of the 1,204 materials, 1,068 bind a `_BaseColorMap`, 1,063 a `_NormalMap` and 1,041 a `_MaskMap`; 194 also bind a `_SpecularColorMap`. The family ships 3,641 texture objects under 2,438 distinct names, 3,639 of them with pixel data streamed to a side `.resS`. Author to DXT5 (2,145 textures) or DXT1 (1,438) at 2048x2048 (2,091), 1024x1024 (904) or 512x512 (403); BC7 appears 48 times, RGBA32 nine and RGB24 once.

### Worked example: the 5.56 PMAG family

This is the case that burns people, so here it is end to end with every bundle named. All filenames below were checked against the live folder `D:\Games\OPERATOR\OPERATOR_Data\StreamingAssets\aa\StandaloneWindows64`, and all 33 PMAG-related bundles currently match the hashes recorded in `catalog_strings.txt`.

Eight prefabs, three separate mesh sets, and none of the eight names its material.

```
PREFAB (weaponmods_stripped_assets_…)                       size B  meshes                                       mat slots
556x45pmag_c8a5f05b3d9730fd61dd0b7f388bd329               272,841  Pmag30_01_low, pmag_m3.001, pmag_m3.002              3
556x45pmagfde_0ddf1734ae4a85a2ceacd321753385f8            272,655  Pmag30_01_low, pmag_m3.001, pmag_m3.002              3
556x45pmagwindow_7e615c8e0daf41c8530af15dd2b6a659         370,904  + pmag_m3, pmag_m3_window                            4
556x45pmagwindowfde_572447fb127383504ed54ae6de7a4e17      370,928  + pmag_m3, pmag_m3_window                            4
556x45pmagwindowmcvariant_78c66380a30aa12599afd1a4b079ef3c 371,070 + pmag_m3, pmag_m3_window                            4
556x4540rndpmag_a76e6d661a3f1e4dd27cce55fb7f6b54          332,285  40_Pmag_low, Pmag30_01_low, pmag_m3.001              2
556x4540rndpmagfde_8ea6622e64da43cdc09ce766071b63a7       332,226  40_Pmag_low, Pmag30_01_low, pmag_m3.001              2
556x4520rndpmag_38c13a2ea30d6974ea079422a11c872e          211,590  SM_AACHB_01a_mag, SM_AACHB_01a_mag_follower          2
```

All 24 of those material slots point at the sentinel PPtr and every one of the eight bundles contains zero `Material` objects. The `556x45 20rnd PMAG` is not a PMAG: its meshes are `SM_AACHB_01a_mag` and `SM_AACHB_01a_mag_follower`, an unrelated vendor asset wearing a PMAG label.

The materials that actually paint them, with albedo colours measured by decoding each texture and averaging every pixel:

```
BUNDLE (weaponmodmaterials_assets_…)              MATERIAL      SHADER                     ALBEDO                                   avg RGB
pmag_4efa1c6f9ce988751fd2139336d6f3c6             PMAG          HDRP/Lit                   Pmag30_low_Pmag30_Diffuse   2048 DXT1    (36,35,36)   black
pmag1_cbc9964ff230107398b72ca0e84889ec            PMAG 1        HDRP/Lit                   Pmag30_Base_Color           2048 DXT1    (108,91,71)  tan
pmag_40_4c507fda8bc579d98eb9ee181e25f8fd          PMAG_40       HDRP/Lit                   BLK_PMAG40_low_…_BaseColor   512 DXT1    (47,47,48)
pmag_40fde_04af4ba095c3db40339e7219b6b9b8bc       PMAG_40 FDE   HDRP/Lit                   FDE_PMAG40_low_…_BaseColor   512 DXT1    (92,81,69)
magfde_47c3719b15be528cbad12086a99034b2           mag fde       MilkShaders/Lit-Template   pmag_m3_BaseColor            512 DXT1    (105,95,75)
magmc_84b0ebde3699a248ef7eeab3c52d7e6a            mag mc        MilkShaders/Lit-Template   pmag_m3_BaseColor mc        2048 DXT1    (141,136,121) MultiCam
magblk_0205b848480403be435b3b91ca5ef5d5           magBLK        HDRP/Lit                   SCAR-H_magazine_20_BLACK_…   512 DXT1    (57,56,55)
pmag_d50_4dcc50804c256cdabb62f94d937e5882         pmag_d50      HDRP/Lit                   pmag_d50_BaseColor          1024 DXT5    (33,33,32)
mag_glass_e5f02f84d5577e280e8d6894b05cad13        mag_Glass     HDRP/Lit                   (no Texture2D in the bundle)

shared maps, one copy per sibling bundle
  Pmag30_low_Pmag30_Normal            2048 DXT5   PMAG, PMAG 1
  Pmag30_low_Pmag30_Specular          2048 DXT1   PMAG, PMAG 1
  PMAG_MaskMap                           4 DXT1   PMAG, PMAG 1        all 16 texels = (0,255,255,255)
  PMAG40_low_PMAG_40_Normal           1024 DXT5   PMAG_40, PMAG_40 FDE
  PMAG40_low_PMAG_40_MaskMap           512 DXT5   PMAG_40, PMAG_40 FDE
  pmag_m3_Normal                      1024 DXT5   mag fde, mag mc
  pmag_m3_OcclusionRoughnessMetallic   512 DXT5   mag fde, mag mc
```

Four things fall out of that table. `PMAG` and `PMAG 1` differ only in base colour, black against tan, on identical normal, specular and mask, which is the textbook colourway shape. `PMAG_MaskMap` is 4x4 and all sixteen texels are the same value, so the 30-round PMAG has no working metallic, AO or smoothness map at all; if you author a real HDRP MaskMap for it you are adding detail the shipped asset never had. `magBLK` is not a generic black magazine, its art is `SCAR-H_magazine_20_BLACK_AlbedoTransparency`, and the tree files it as its own item while `mag fde` and `mag mc` are two colourways of an item called `mag` that is a PMAG. And `mag_Glass`, the window insert, ships zero textures and is pure shader parameters; the tree files it under Reticles & scope glass, not Magazines.

The MultiCam magazine is one texture change. `mag mc` and `mag fde` are the same shader with the same normal and the same ORM pack, differing only in `_BaseColorMap`, 2048x2048 against 512x512. That is the whole authoring cost of a patterned magazine.

To repaint the entire 5.56 PMAG line you edit four base-colour textures and nothing else: `Pmag30_low_Pmag30_Diffuse`, `Pmag30_Base_Color`, `pmag_m3_BaseColor`, and the `BLK_`/`FDE_` PMAG40 pair. Watch the shader split while you do it, because `pmag`/`pmag1`/`pmag_40` are HDRP/Lit with a `_SpecularColorMap` and a `_MainTex` alias, and `magfde`/`magmc` are `MilkShaders/Lit-Template` with an ORM mask and no `_MainTex`. A mask authored for one reads wrong in the other.

Finish there and the gun looks right while the plate carrier does not. The magazine in the rig is a different asset in a different family: `charactermodmaterials_assets_mi_pmag_556_e6388d7446e5ff293e3a55fcdeea5258.bundle`, material `MI_PMAG_556`, albedo `PMAG_Black_BaseColor` at 1024x1024 with a 2048x2048 `MI_PMAG_556_MaskMap`. Different material name, different texture names, different resolutions. The character world carries six PMAG material bundles (`mi_pmag_556`, `m4pmag_sdr`, `mi_m4pmagfde`, `mi_mag_magpul_pmag_gen3_m3_556_30_blk`, `…_tan`, `…_m3_glass`) and you have to ship all of them for a magazine repaint to look finished. Match by eye, not by copying files; the UVs come from different vendor packs.

### Is there a camo layer on weapons?

No. There is a colourway system and a solid-colour tint path, and neither can express a pattern.

The code side settles the layer question. The case-sensitive string `Camo` occurs exactly once in the IL2CPP identifier table, at `Editor_AddClothingCamos`, which is clothing. A case-insensitive search returns five more hits and all five are unrelated (`EnterCaMode`, `ExitCaMode`, `camOffset`, `m_VcamOwner`, `_aecAmount`). There is no `weaponSkin`, `skinIndex`, `SkinMaterial` or `paintJob` identifier anywhere. Nothing exists to drive a pattern, detail-tiling or decal layer on a weapon.

The art side quantifies it. Across the 1,204 weapon materials the tree resolves 12 distinct colourway labels including `(default)`; the 1,036 character materials resolve 25. On the character side the pattern labels run AOR1 64, AOR2 49, M81 Woodland 41, MARPAT 14, Tigerstripe 13, Flecktarn 2. On the weapon side every one of those is zero, and searching every weapon material name, slug and bound texture name for pattern tokens returns eight MultiCam materials and nothing else. The token `camo` appears in zero weapon material names.

One qualification, because it is the kind of thing that makes a flat "zero AOR1" wrong. Four `weaponmods_stripped` prefab slugs do carry `aor1`, all of them M79 parts: `m79barrelsawedoffaor1`, `m79lockaor1`, `m79sawedoffbodyaor1`, `m79triggerguardaor1`. No weapon material or texture name carries an AOR1 token. The likely candidates are `mi_gl_m79_devgru_body`/`_barrel` against `mi_gl_m79_devgru_bodyclassic`/`_barrelclassic`, and decoding all four shows a light desert-painted pair against a dark pair rather than a digital pattern. The prefab-to-material mapping is not recoverable from the files and needs a runtime probe.

Those eight MultiCam materials compose one rifle, plus two strays. Decoding all eight and measuring per-channel spatial variance confirms every one is a multi-colour pattern; the FX-19 slide is the darker MultiCam Black.

```
MULTICAM ON WEAPONS, the complete set
  receiver   m4mc_8cdbe223aeb7a24dc2235908770a39b0        M4 MC              M4_BaseMap MC                          2048
  handguard  m4x_hg_dd952_…                               M4X_HG_DD95 2      M4X_HG_DD95_MC_Base_Color              2048
  grip       gripmc_63945da168fc763d05f715ce2bf9c9b3      gripMC             pistol_grip_A2_MC_AlbedoTransparency    2048
  stock      lmtsopmodmc_f7a062786971c8a93e1304fc688c24db lmt sopmod mc      stock_LMT_SOPMOD_AlbedoTransparency_MC  2048
  optic      exps3mc_02c925bb3a6820b75c42b3c7007adbaa     EXPS3 MC           EXPS3_MC_AlbedoTransparency            2048
  magazine   magmc_84b0ebde3699a248ef7eeab3c52d7e6a       mag mc             pmag_m3_BaseColor mc                   2048
  (stray)    mod31_…                                      Mod3 1             Mod3_MC_AlbedoTransparency             2048
  (stray)    fx19mcb_a7c04b71446cf88004388661b5598ab8     FX19 MCB           fx19 slide_FX_19_BaseMap               2048
```

Nine MultiCam prefabs exist (`coltupperrecievermcvariant`, `coltlowerrecievermcvariant`, `dd_ris_iimcvariant`, `bcmgunfightermcvariant`, `bcm_gunfightermcflippedvariant`, `std_ar15-pistolgripmcvariant`, `b5systemslmtsopmodstockmcvariant`, `eotechexps-3mcvariant`, `556x45pmagwindowmcvariant`), and the pairing of six materials to those nine prefabs is inference from naming, since the prefabs name no material. Note the handguard: the MultiCam DD rail material is named `M4X_HG_DD95 2`, with no colour token in the material name at all, hidden behind a Unity duplicate suffix. Only the texture name gives it away. `Mod3 1` is worse. It sits in Other, `classified.json` cannot identify the part, and its sibling `Mod3` is a solid dark material at 1024x1024 against the MultiCam sibling's 2048x2048.

The two prefabs with `camo` in the slug are not a pattern system either. `b5systemsbravostockblkcamo` and `b5systemsbravostockodcamo` are two of seven authored SKUs of one B5 Systems Bravo stock (the others are `blk`, `coyotebrown`, `fde`, `grey`, `odgreen`). Seven material bundles `bravob5` through `bravob56` exist, all sharing one `LitMask` and one `b5bravo_NormalOpenGL` byte for byte and differing only in albedo, so the pairing is near-certain but is still inference. Decoding all seven and measuring per-channel spatial variance against known controls, MultiCam albedos score standard deviations of 20 to 45 with 27 to 61 distinct colour bins while these score 2.5 to 13 with 3 to 12 bins, except one. `bravob55` (albedo `b5bravo_BaseColor 5`, sd 38.1/32.0/21.5, 23 bins) is the only multi-colour camouflage pattern in the set. The other six average out to black, dark grey, coyote brown, tan, olive drab and grey-blue. Two prefabs are called camo and only one pattern exists, so at least one of them cannot be getting one. Which prefab consumes `bravob55` needs a runtime probe.

There is a tint path, and it is the closest thing to a colour layer on the weapon side, but it is solid-colour only. The FX-19 pistol body ships three colourways that bind no albedo at all: `fx19blk`, `fx19gry` and `fx19golden` bind only `1_FX_19_Normal` on `_NormalMap` and `1_FX_19_Emission` on `_EmissiveColorMap`, and carry their colour in `_BaseColor` as `0.1604,0.1604,0.1604`, `0.3396,0.3396,0.3396` and `0.7170,0.4633,0.0000`, with `_Metallic` shifting from 0.598 on black to 0.407 on the other two. The Glock 17 frame does the same over a neutral albedo: `glock17_framefde`, `glock17_framewhite`, `glock17_framegld` and `glock17_framegrn` all bind the identical texture `GLOCK17_Frame_Base_color_Wht`, the identical `Frame Mask` and the identical `GLOCK17_Frame_Normal_DirectX`, and differ only in `_BaseColor`, at `0.4235,0.3686,0.2314`, `0.4717,0.4717,0.4717`, `0.8118,0.6510,0.2510` and `0.3529,0.4039,0.2235`. This is the same mechanism as the plain T-shirt colours on the character side. A float4 cannot carry a pattern.

What that means for a camo-painted rifle. You ship one new `weaponmodmaterials` bundle per part per pattern, exactly as the developers did, and you get the attachment definition to select it. Nine bundles buys you one complete rifle in one pattern, and the MultiCam set is a working template proving the whole path. There is no shortcut and no shared pattern layer to hook, so a pattern across the whole weapon catalogue means roughly 1,204 authored materials and is not tractable by hand. If you want breadth for effort, start with the generic materials whose names imply they are shared across many prefabs: `gripblk`/`gripfde`/`gripmc`, `magblk`/`magfde`/`magmc`, `buttblk`/`buttfde`, the `black`/`black1`/`black2` trio and the `steel`/`steel1`/`steel2` trio. How many prefabs each one actually paints is not recoverable from the files.

### Naming traps that cost the most time

Of the 1,204 material names, 310 end in a Unity duplicate-name suffix, and that suffix is where the colourways hide. The 10 inch handguard is the clean demonstration. `handguard_10`, `handguard_101` and `handguard_1011` are three separate bundles, two of them containing a material literally named `handguard_10` and the third holding `handguard_10 1`, all three binding a texture named `handguard_10_BaseColor` at 1024x1024. All three ship the identical `handguard_10_Normal` and `handguard_10_OcclusionRoughnessMetallic`, and the three albedos are three different images averaging (73,71,67), (106,94,81) and (85,84,80), which reads as dark, FDE and grey. No colour token appears anywhere in the material name, the slug or the texture name, and the vendor is not resolvable from the files.

The same applies to the Magpul CTR. Four bundles, `magpul_ctrblk`, `magpul_ctrfde`, `magpul_ctrgray` and `magpul_ctrgray1`, all bind `magpul_ctr_BaseColor` at 2048x2048 and all carry `_BaseColor` of pure white, so the colour is entirely in each bundle's own copy of the texture. The four copies hash differently, averaging (47,46,45), (116,102,86), (64,65,54) and (63,63,65). The two materials both named `magpul_ctr Gray` are not the same grey; one leans olive.

Shared texture names usually do mean shared art, but not often enough to trust. 765 of the 2,438 distinct weapon texture names appear in more than one bundle. Decoding every copy of all 765 and comparing pixels, 594 names resolve to one image everywhere and 171 do not. Worse, 140 of those 171 collide at the same resolution, so metadata will not warn you; only 31 differ in size. Never dedupe weapon textures by name across bundles, and never assume the copy you edited is the one the game loads.

One shipped wiring bug is worth knowing because it looks like a texture problem. The bundle `rail_ar_mk4_721` holds a material named `rail_ar_mk4_7 2` that binds `rail_ar_mk4_7_SpecularSmoothness` to both `_BaseColorMap` and `_MainTex`, a specular map in the albedo slot, and the bundle ships no albedo texture at all. A material with the identical name `rail_ar_mk4_7 2` lives in bundle `rail_ar_mk4_72` and binds `rail_ar_mk4_7_AlbedoTransparency` correctly. That copy and the one in `rail_ar_mk4_7` are pixel-identical at 2048x2048, while `rail_ar_mk4_71` binds a different 1024x1024 image; the naming implies a downscale of the same art, unverified.

The Reticles & scope glass category, 71 items and 75 materials, is not a finish branch. Forty of the items are reticles and the rest are lens, glass, render-target and holdout materials. Editing them changes the sight picture, not the housing colour, and breaking one breaks aiming. They are the largest group of materials that survive inside prefab bundles, but they are not the only one; body, ammunition and decal materials do too, as listed above.

Skip the dead weight. `modplaceholdermaterial` (material name `mod placeholder material`), the five `newmaterial` through `newmaterial4` bundles, `default1`, and the particle strays that sit in this family (`muzzleflash1`, `2`, `4`, `5`, note the missing 3, plus `muzzleparticles`, `smoke`, `flame1`, `flame2`, `distortion`, `919_bullet`, `919_case`, `primer_new_silver`) are not attachment finishes. The `bolt` bundle is not dead weight despite the name; it is the one bundle in the family that holds two materials, `MI_Body` and `M4`, both HDRP/Lit.

Finally, the 32 hex content hash in every bundle filename changes when the game patches. Locate bundles by slug against the live folder every time, never from a saved list, or your override stops applying with no error.

## 7. Patches, insignia, optics and reticles

Two small-surface subsystems sit at opposite ends of the difficulty curve. A patch is one quad, one material, one texture, and the acceptance test between item and socket is a string comparison, which makes a custom patch the cheapest complete mod in the game. A reticle is also one texture, but it renders through a second camera, a custom shader with a property set that changes name between families, and a per-optic saved index. Both are mapped below.

### 7.1 The patch surface

The character tree category `Patches & insignia` holds 27 items across 31 material variants, but the category is a classifier bucket. It also contains two helmet skins (`MI_SuperHighCut_AC_blk flag`, `MI_SuperHighCut_MC USFLAG`), two T-shirt prints on `Shader Graphs/ClothWind` (`middle east`, `stupid`), a five-colourway `Vet` set on `MilkShaders/Lit-Template`, and a TextMeshPro font atlas material (`amarurgt SDF Material`). It also misses four of the twelve Patreon patch materials. Do not use it as the patch inventory.

The load-bearing counts are different. There are 20 equippable insignia prefabs, and 21 of the 1,025 `charactermodmaterials` bundles carry an Editor source path under a `PATCHES` folder. Reading the `AssetBundle` object's `m_Container` in each material bundle recovers that path, which separates most patch materials from garment materials that merely have "patch" in a texture name.

```
Assets/1Assets/1CGTrader/3DMA/WSF Operator V2/Textures/PATCHES/PBR_Patch_US_CO_tx/USA.mat
     -> charactermodmaterials_assets_usa_e045bb1013bab69c9314acd800274550.bundle
Assets/1Assets/1CGTrader/3DMA/WSF Operator V2/Textures/PATCHES/Patreon/syko.mat
     -> charactermodmaterials_assets_syko_ac5ec1a974096018263d2be4c372e480.bundle
Assets/_customArtAssets/_Patches/Country/FLAGS VOL 1/USA/usa ir.mat
     -> charactermodmaterials_assets_usair_40132777c4fecc795779b1415869b3e1.bundle
```

The path test is a filter, not a complete index. Four real patch materials fail it because they were authored inside their host carrier's asset folder rather than a patches folder: `MI_Patch_3DMA_US_CO` under `3DMA/Ferro FCPC`, `MI_Patch_3DMA_US_IR` under `3DMA/TYR PICO 1M ASSAULTER`, `MI_Patch_US_Snake_02` under `3DMA/MSV/Textures`, and `Patch_3DMA_sdr 1` under the JPC V2 texture tree. Run the path scan, then add those four by hand.

Eight of the 20 prefabs are named with a leading apostrophe, a sort-to-top trick: `'USA`, `'USA Covert`, `'USA IR`, `'JTAC`, and the four DEVGRU squadron patches. The other twelve are the Patreon morale patches.

### 7.2 Sockets, and the one string that gates them

A patch mounts because two serialized strings match. The socket is a `CharacterModParent` MonoBehaviour of about 200 bytes carrying exactly two strings, `ModParentName` (the UI label) and `Mod_Compatibility`; every patch socket in the game sets `Mod_Compatibility = "Patch"`. The item is a `CharacterMod` whose `Compatibility` is also `"Patch"`. There is no enum, no id table and no registration step, so a custom item becomes socketable by setting that one string, and a garment accepts patches it currently refuses by gaining a child with that one string. For contrast the adjacent socket on the same AVS vest reads `ModParentName = "Armor Plates"` with `Mod_Compatibility = "ArmourPlate"`.

488 of the 1,165 `charactermods_stripped` garment prefabs carry patch geometry or a patch socket. Counts below are distinct prefab bundles carrying that GameObject, measured by opening all 1,165.

| `ModParentName` | GameObject | Prefabs | Where |
|---|---|---|---|
| Vest Patch Main | `MOD_PatchMain` | 242 | AVS 66, Ferro 37, TYR PICO 29, JPC 28, Mystery Ranch 18, NFM 12, Spiritus 12, rest scattered |
| Left Arm Patch | `MOD_LeftArmPatch` | 142 | Patagonia 22, CRYE G2 21, CRYE G2 Rolled 17, PCU Hooded 18, Rugby 39, PCU Jacket 9, CRYE G3 11, misc 5 |
| Right Arm Patch | `MOD_RightArmPatch` | 142 | same carriers |
| Left Arm Patch 2 | `MOD_LeftArmPatch2` | 66 | PCU Hooded 18, PCU Jacket 9, Rugby 39 |
| Right Arm Patch 2 | `MOD_RightArmPatch2` | 66 | same carriers |
| Left Arm Patch High | `MOD_LeftArmPatchHigh` | 38 | CRYE G2 21, CRYE G2 Rolled 17 |
| Right Arm Patch High | `MOD_RightArmPatchHigh` | 38 | same carriers |
| Left Arm Middle Big Patch | `BigPatch` | 27 | PCU Hooded 18, PCU Jacket 9 |
| Right Arm Middle Big Patch | `R BigPatch` | 27 | same carriers |
| Patch | `MOD_Patch` | 24 | ball caps, forward and backwards variants |
| Patch Left / Patch Right | `MOD_Patch Left` / `MOD_Patch Right` | 10 each | Ops-Core FAST XP 9, ATE HHV Ballistic 1 |
| Left Arm Patch High / Right Arm Patch High | `MOD_LeftArmPatch High` / `MOD_RightArmPatch High` | 2 each | CRYE G3 Combat Shirt, plain and rolled |

Note the last row. The CRYE G3 combat shirt spells its high socket with a space before `High`, so a name match on `MOD_LeftArmPatchHigh` misses it. Match on `ModParentName` instead.

The raw GameObject names are polluted by Unity's FBX-merge concatenation, for example `PCU Jacket Grey_PCU Jacket Grey_Rubgy Long Tucked FDE_..._MOD_LeftArmPatch (1)`. Read `ModParentName` out of the MonoBehaviour. Separately, meshes such as `Vest_TYR_PICO_AssaultersPC_Patch` (19 prefabs) and `SKM_AVS_Minimap_Patch` (16) are baked patch geometry, not sockets, and take a material from the garment's own list.

### 7.3 The prefab does not hold the material, an Addressables GUID does

Every patch prefab's MeshRenderer points its single material slot at the same external PPtr `(m_FileID=2, m_PathID=-5343436609273763532)`, which resolves into the shared CAB that every character prefab depends on. That is a placeholder. The real material is carried as a 96-byte MonoBehaviour holding one 32-hex Addressables GUID, loaded by Addressables and assigned by `CharacterMod.ActuallySetMaterial()` at runtime. Editing a patch prefab bundle therefore does nothing, and editing the shared CAB would hit every garment in the game. The only correct edit target is the material bundle.

Those GUIDs resolve. In `catalog.bin` each GUID is followed immediately by a little-endian length-prefixed asset name, so `raw.find(guid)` then `struct.unpack_from('<I', raw, i+32)` recovers the material; the whole file yields 4,890 GUID-to-name pairs. Reading the GUID out of every patch prefab bundle and resolving it gives the complete item to material map.

| Item prefab | Material | Material bundle slug |
|---|---|---|
| `'USA` | `USA` (catalog name `PBR_Patch_US_CO_tx/USA.mat`) | `usa` |
| `'USA Covert` | `MI_Patch_US_BW` | `mi_patch_us_bw` |
| `'USA IR` | `MI_Patch_US_BW 1` | `mi_patch_us_bw1` |
| `'JTAC` | `MI_Patch_JTAC` | `mi_patch_jtac` |
| `'DEVGRU Gold Squadron ''Crusader''` | `Patch Golden SQ` | `patchgoldensq` |
| `'DEVGRU Gold Squadron ''Lion''` | `Patch Golden SQ` | `patchgoldensq` |
| `'DEVGRU Red Squadron ''Demon Hunter''` | `Patch Golden SQ` | `patchgoldensq` |
| `'DEVGRU Red Squadron ''Tribe''` | `Patch Golden SQ` | `patchgoldensq` |
| `PatreonPatch ''ap_amazing''` | `APAmazing` | `apamazing` |
| `PatreonPatch ''big_mamas_house''` | `bigmamashouse` | `bigmamashouse` |
| `PatreonPatch ''doc'minty''` | `docminty` | `docminty` |
| `PatreonPatch ''dope''` | `dope` | `dope` |
| `PatreonPatch ''greyfox''` | `GreyFox` | `greyfox` |
| `PatreonPatch ''loins4sale''` | `Loins` | `loins` |
| `PatreonPatch ''nikkoortizzz''` | `nikkkooooooootirz` | `nikkkooooooootirz` |
| `PatreonPatch ''sygic''` | `sygic` | `sygic` |
| `PatreonPatch ''syko''` | `syko` | `syko` |
| `PatreonPatch ''waterboardinginstructor''` | `waterboardinginstructor` | `waterboardinginstructor` |
| `PatreonPatch ''wileecoyote''` | `wile e coyote` | `wileecoyote` |
| `PatreonPatch ''willy''` | `Willy` | `willy` |

All four DEVGRU items share one material. Repainting `patchgoldensq` changes all four at once, and because each of the four meshes occupies a different region of that one sheet, you can still author them independently inside a single texture.

Blast radius matters more than the item table suggests. Building a GUID-to-name map from `catalog.bin`, then scanning all 1,165 `charactermods_stripped` bundles for those GUIDs, gives the full reference set. Three of the equippable patch materials are also bound by garment prefabs: `MI_Patch_US_BW` by ten AVS PL Radio and Rifleman V3 vests as well as `'USA Covert`, `MI_Patch_US_BW 1` by five AVS SNOT vests as well as `'USA IR`, and `MI_Patch_JTAC` by the AVS JTAC vest as well as `'JTAC`. Repaint any of those three and the vests change with them.

The remaining patch materials never reach a standalone item and are referenced only by garments. `MI_Patch_US_IR` is used by 8 LBT-6094 Breacher and Rifleman vests, `blackbeard` by 11 AVS PL Leader and PL Sgt vests, `Seal Team` by 4 LBT-6094 Squad Leader vests, `MI_Patch_US_Snake_02` by `msvgen2squadleadermc`, `MI_Patch_3DMA_US_IR` by `tyrpico1massaultercoyotevariant`, `Patch_3DMA_sdr 1` by `jpcbreacherblk`, and `usa ir` by `usav5`. `MI_Patch_3DMA_US_CO` is referenced by nothing at all, which makes it 6.3 MB of dead weight and the one patch material you can repurpose without displacing existing content.

### 7.4 Resolution, format and the mip bug

All patch textures are BC compressed. Base colours are DXT1, normals and masks DXT5 with two DXT1 exceptions. Six of the 23 textures across the nine patch material bundles ship `m_MipCount = 1`, so those patches alias at distance.

| Material (bundle slug) | Property | Texture | Res | Format | Mips |
|---|---|---|---|---|---|
| `MI_Patch_3DMA_US_CO` (`mi_patch_3dma_us_co`) | `_BaseColorMap` | `Patch_BaseColor_US_CO` | 2048 | DXT1 | 12 |
| | `_NormalMap` | `Patch_Normal_US_CO` | 2048 | DXT5 | 12 |
| | `_MaskMap` | `MaskMap` | 2048 | DXT5 | 12 |
| `MI_Patch_US_BW` (`mi_patch_us_bw`) | `_BaseColorMap` | `Patch_US_BW_BaseColor` | 2048 | DXT1 | **1** |
| | `_NormalMap` | `Patch_US_BW_Normal` | 1024 | DXT5 | 11 |
| | `_MaskMap` | `patch_us_mask` | 512 | DXT5 | 10 |
| `MI_Patch_US_IR` (`mi_patch_us_ir`) | `_BaseColorMap` | `Patch_US_IR_BaseColor` | 2048 | DXT1 | **1** |
| | `_NormalMap` | `Patch_US_IR_Normal` | 1024 | DXT5 | 11 |
| | `_MaskMap` | `patchusmask` | 1024 | DXT5 | 11 |
| `MI_Patch_3DMA_US_IR` (`mi_patch_3dma_us_ir`) | `_BaseColorMap` | `Patch_US_IR_BaseColor` | 2048 | DXT1 | **1** |
| | `_NormalMap` | `Patch_US_IR_Normal` | 1024 | DXT1 | 11 |
| `MI_Patch_US_BW 1` (`mi_patch_us_bw1`) | `_BaseColorMap` | `Patch_US_IR_BaseColor` | 2048 | DXT1 | **1** |
| | `_NormalMap` | `Patch_US_IR_Normal` | 2048 | DXT5 | 12 |
| | `_EmissiveColorMap` | `Patch_US_IR` | 1024 | DXT1 | 11 |
| `MI_Patch_JTAC` (`mi_patch_jtac`) | `_BaseColorMap` | `Patch_JTAC_BaseColor` | 1024 | DXT1 | **1** |
| | `_NormalMap` | `Patch_JTAC_Normal` | 512 | DXT5 | 10 |
| | `_MaskMap` | `jtac_mask` | 512 | DXT5 | 10 |
| `Patch Golden SQ` (`patchgoldensq`) | `_BaseColorMap` | `Patch_Golden_SQ_BaseColor` | 1024 | DXT1 | **1** |
| | `_NormalMap` | `Patch_Golden_SQ_Normal` | 1024 | DXT5 | 11 |
| `MI_Patch_US_Snake_02` (`mi_patch_us_snake_02`) | `_BaseColorMap` | `Patch_Snake_BaseColor` | 1024 | DXT1 | 11 |
| | `_NormalMap` | `Patch_Snake_Normal` | 1024 | DXT1 | 11 |
| `Patch_3DMA_sdr 1` (`patch_3dma_sdr1`) | `_BaseColorMap` | `Patch_3DMA_BaseColor` | 2048 | DXT1 | 12 |
| | `_NormalMap` | `Patch_3DMA_Normal` | 2048 | DXT5 | 12 |

Every one of those nine materials also binds `_MainTex` to the same texture as `_BaseColorMap`, except `MI_Patch_3DMA_US_CO` which binds only `_BaseColorMap`. That is the HDRP/Lit legacy alias written by the importer, not a second slot to author.

The twelve Patreon materials are simpler and better behaved: HDRP/Lit, no mask, a full DXT1 albedo mip chain, and a shared 1024 DXT5 `PatchesL_T_Patches_Normal` copied into all fourteen bundles that use it (the twelve Patreon patches plus `blackbeard` and `Seal Team`). Nine ship a 2048 albedo with 12 levels; `sygic`, `waterboardinginstructor` and `Loins` ship 1024 with 11; `Willy` ships 512 with 10. Authoring a replacement at the stock dimensions is a drop-in, but find the texture by reading the bundle rather than by guessing the name, because the albedo name rarely matches the material: `docminty` binds `dumbass minty`, `nikkkooooooootirz` binds `NikoOrtiz`, `Loins` binds `LoinsForSale`, `Willy` binds `Willy Patch`, `bigmamashouse` binds `big_mamas_house`.

Eight of the nine patch materials are HDRP/Lit. `MI_Patch_3DMA_US_CO` is `MilkShaders/Lit-Template`, the studio's own layered variant, and carries `_BASE_LAYER_TRIPLANAR`, `_DETAIL_TRIPLANAR_UV`, `_HeightMapParametrization` and `_DisplacementLockObjectScale` in place of the HDRP/Lit subsurface and iridescence set. If you rebuild a patch material from scratch rather than editing one in place, pick the matching shader or the patch renders wrong.

Name-based batch replacement across bundles is unsafe here. `Patch_BaseColor_US_CO` and `Patch_Normal_US_CO` each exist twice at different resolutions (2048 in `mi_patch_3dma_us_co`, 1024 in `usa`), `Patch_US_IR_Normal` exists three times at three different resolution and format combinations, and the texture named simply `MaskMap` has 274 copies build-wide across three resolutions, 265 of them in `charactermodmaterials`. Key on the bundle, never on the texture name.

### 7.5 What the art actually shows

Decoding the shipped albedos contradicts several material names. `Patch_US_IR_BaseColor`, bound by `MI_Patch_US_IR`, `MI_Patch_3DMA_US_IR` and `MI_Patch_US_BW 1`, is a **France** tricolour IR flag patch on multicam backing, with FRANCE stencilled down the hoist. `Patch_US_BW_BaseColor` is a subdued **FR** French flag patch. `Patch_JTAC_BaseColor` is a multicam patch carrying a three-character stencil callsign that reads **3B9**, not a JTAC device. `Patch_Golden_SQ_BaseColor`, the material behind all four DEVGRU items, is a 1024 sheet of four French patches: a 1er RPIMa/SAS "QUI OSE GAGNE" para shield, a circular tan griffin, a FRANCE tricolour shield, and a red square griffin. Only `MI_Patch_3DMA_US_CO`, `USA` (both on `Patch_BaseColor_US_CO`) and `usa ir` (on `PatchesGenerator_Albedo`) carry an actual US flag in the albedo, and `MI_Patch_US_BW 1` carries one only in its emissive map. Anyone building a "restore the US patches" mod should start from the pixels, not the names.

`MI_Patch_US_BW 1` is the only patch material that binds `_EmissiveColorMap`. It points at `Patch_US_IR`, a white US flag silhouette on black, and sets `_UseEmissiveIntensity = 1`, but ships `_EmissiveIntensity = 0`, `_EmissiveColor = (0,0,0,0)`, `_EmissiveExposureWeight = 0` and `_UseEmissiveMap = 0`. The glow is authored and switched off at rest. The IL2CPP metadata contains a first-party script `\Assets\Scripts\IRPatch.cs` and a type `IRPatch`, and nothing else in the build carries that name; the reading that `IRPatch` drives `_EmissiveColor` and `_EmissiveIntensity` when night vision comes up is inference from the name and the zeroed values, not something confirmed at runtime. Note that `usa ir` also sets `_UseEmissiveIntensity = 1` and `_AlbedoAffectEmissive = 1` without binding an emissive map, so the emissive path is not unique to `MI_Patch_US_BW 1`, only the map binding is.

Three patch materials ship a non-zero `_Metallic`: `MI_Patch_US_BW 1` at 0.651 (with `_Smoothness` 0.631), `Patch Golden SQ` at 0.5 and `USA` at 0.5. Everything else in the family sits at `_Metallic` 0 and `_Smoothness` 0.5. `MI_Patch_JTAC` and `MI_Patch_US_BW` are the only two with a tinted `_BaseColor` at (0.906, 0.906, 0.906, 1); the rest are pure white.

### 7.6 Mesh archetypes and UV convention

There are three patch mesh archetypes and they are shared, not per item. UV islands below were computed by union-find over the exported mesh triangles and checked by plotting the UV points back onto the decoded texture. Pixel figures use a top-left origin.

```
CustomPatch                    76 verts   all 12 Patreon morale patches
  local AABB extents (0.0046, 0.0339, 0.0228) m  = 68 x 46 mm, ~9 mm thick
  full UV bbox   u[0.0050..0.9908]  v[0.6951..0.9950]   6 islands
  front island   u[0.4758..0.7555]  v[0.7001..0.8782]
  on a 2048 sheet:  x[974..1547]  y[249..614]    (573 x 365 px, ~3:2)

SKM_CRYE_Shirt_LS_Patch_Flag   118 verts  'USA, 'USA Covert, 'USA IR, 'JTAC
  full UV bbox   u[0.0036..0.9994]  v[-0.0003..0.7087]   2 islands
  front island   u[0.0036..0.5081]  v[0.0029..0.7087]
  back island    u[0.5102..0.9994]  v[-0.0003..0.6910]
  on a 1024 sheet:  front x[4..520]  y[298..1021]      (517 x 723 px, portrait)
  MI_Patch_JTAC ships a 1024 albedo; the other three ship 2048, so double those
  pixel figures for 'USA, 'USA Covert and 'USA IR

silhouette-cut DEVGRU meshes, all four on Patch Golden SQ (1024 sheet)
  artwork region = UVs falling on the printed patch; the rest of each mesh
  scatters small edge islands across the sheet
  Lion_Cube.127     280 v   x[19..368]   y[219..580]   -> SAS "QUI OSE GAGNE" shield
  Indian_Cube.124    96 v   x[423..870]  y[195..620]   -> circular tan griffin
  Crusade_Cube.125  228 v   x[443..763]  y[632..1005]  -> FRANCE tricolour shield
  Horns_Cube.128    166 v   x[19..440]   y[584..1005]  -> red square griffin
                            (166 verts spread over 33 islands)
```

The consequence for authoring is that no patch mesh uses the full 0-1 square. A Patreon morale patch shows only the top band of its sheet and only the middle of that band horizontally; a centred 0-1 design painted there renders off-patch. The flag mesh uses the left half and the bottom seventy percent, with the back face on the right half. The four DEVGRU meshes are cut to their silhouette, so a rectangular replacement will not read correctly on them.

To reproduce or check any of these, load the prefab bundle, export the mesh, and plot `vt` lines onto the texture at `(u*W, (1-v)*H)`.

### 7.7 Procedure: shipping a custom patch

Route A is a texture swap with no code, the same route the community GIGN retexture pack reportedly took across ten `charactermodmaterials` bundles. That pack is cited as precedent, not verified here.

1. Re-scan the live folder for the current filename. The 32-hex suffix is a content hash, and a bundle sitting under a stale hash is never loaded, so the mod does nothing and reports nothing. Right now the trap is dormant: all 4,989 live hashes still match the July snapshot. It is not idle in general, because the 2026-07-06 patch rewrote 1,108 bundles in one day. Store the slug, resolve the hash at install time.

   ```powershell
   Get-ChildItem "D:\Games\OPERATOR\OPERATOR_Data\StreamingAssets\aa\StandaloneWindows64" `
     -Filter "charactermodmaterials_assets_mi_patch_us_bw1_*.bundle"
   ```

2. Pick the correct material bundle from the item table in 7.3, and check 7.3's reference list first, because three of the equippable patch materials are also worn by AVS vests. Editing the prefab bundle has no effect.

3. Open the bundle and read the existing texture so you author to the same specification. The shader stub in the bundle carries an empty `m_Name`, but the real name survives on `m_ParsedForm`:

   ```python
   import UnityPy
   UnityPy.config.FALLBACK_UNITY_VERSION = "6000.3.8f1"   # bundles are version stripped
   env = UnityPy.load(SRC)
   for obj in env.objects:
       if obj.type.name == "Material":
           mat = obj.read()
           print(mat.m_Name, mat.m_Shader.read().m_ParsedForm.m_Name)   # -> 'HDRP/Lit'
       elif obj.type.name == "Texture2D":
           t = obj.read()
           print(t.m_Name, t.m_Width, t.m_Height, int(t.m_TextureFormat), t.m_MipCount)
   ```

4. Paint into the island rectangle from 7.6, at the stock resolution and format. Leave the rest of the sheet as it was; other meshes and other UV islands may be reading it.

5. Write the image back and repack. `set_image` defaults to `mipmap_count=1`, so a naive swap strips the mip chain from every texture you touch:

   ```python
   from PIL import Image
   img = Image.open("my_patch.png").convert("RGBA")
   tex.set_image(img, target_format=int(tex.m_TextureFormat), mipmap_count=10)
   tex.save()
   open(SRC, "wb").write(env.file.save(packer="lz4"))
   ```

   UnityPy stops generating levels at 4x4 for block-compressed formats. Round trips confirm the shortfall: a 2048 texture asked for 12 comes back with 10, and a 1024 texture asked for 11 comes back with 9. Pass the largest count you can and accept the two-level shortfall, or supply your own chain. Generating any chain at all on the six `mips = 1` base colours listed in 7.4 is a free quality win no official update has made.

6. Overwrite the live file under its current name. Addressables performs no integrity check on these bundles; the per-bundle CRC field in `catalog.bin` is zero, and a repacked bundle loads even when it grows by half.

Route B adds a new patch without touching `catalog.bin`, which you cannot rewrite. Hijack an existing Patreon patch: its prefab already carries the `CustomPatch` mesh, `Compatibility = "Patch"` and a unique material GUID that nothing else references, so repainting that material's textures makes the item yours. It keeps its UI name unless you also edit the prefab's display name override.

Route C is a MelonLoader postfix on `CharacterMod.ActuallySetMaterial()`, branching on `Compatibility == "Patch"` and assigning a Material you built. A material pulled out of a bundle arrives with a broken shader reference and needs `mat.shader = Shader.Find(name)`; supply the name from `m_ParsedForm` as in step 3.

One item in the live folder is worth flagging rather than concluding on. `Patch_Select_Russia.jpg` is the only non-bundle file returned by a `*patch*` glob on the Addressables folder, and it appears nowhere in the catalog. A loose JPEG is not something an Addressables build produces, so this is most likely a file dropped there by a person. Verify against a fresh `steam://validate/1913370` before treating it as evidence of a cut Russia patch.

### 7.8 Reticles: the shipped set

Across the full scan, 60 materials bind a reticle texture: 40 on a property named `_Reticle` and 20 on a property named `Reticle`, with a further 11 materials exposing `Reticle` bound to nothing (the render-target and scope-effect materials, plus the dead `razorhdg2e16_reticle`). Each of the 60 lives in its own bundle with its own physical copy of the texture, so editing one bundle affects only that bundle. 56 of the 60 are in `weaponmodmaterials`. The other four are inside optic **prefab** bundles (`eotechexps-3blk`, `eotechexps-3fde`, `eotechexps-3mcvariant`, `eotechg33magniferblk`), each carrying a material named `EXPS3_reticle` on `Shader Graphs/Reticle` alongside separate `EXPS3_reticle 1` material bundles that sit on `Ultimate Scope Shaders/HolographicSight`. Which of the pair renders for the EOTech EXPS-3 is unverified and needs a runtime component dump.

Those 60 materials draw on 23 distinct texture names but 25 distinct physical specifications, because `Artboard 1` exists as both 1024 DXT5 with 11 mips (in `reticle1` and `reticle2`) and 512 DXT5 with 1 mip (in `tango6t_reticle`), and `reticle` exists at 2048 DXT5 with both 1 and 12 mips. Never dedupe reticle textures by name across bundles.

| Texture | Res | Format | Mips | Materials | Bundle copies |
|---|---|---|---|---|---|
| `Reticle03` | 1000x1000 | DXT5 | 1 | 15 | 15 (14.3 MB total) |
| `reticle_1dot` | 512x512 | RGBA32 | 1 | 11 | 11 (11.0 MB total) |
| `ROMEO8T_red_dot_reticle_Emission` | 512x512 | DXT5 | 1 | 4 | 4 |
| `Artboard 1` | 1024 / 512 | DXT5 | 11 / 1 | 3 | 3 |
| `triangle_reticle` | 2048x2048 | DXT5 | 1 | 2 | 2 |
| `OKP7 Reticle` | 2048x2048 | DXT5 | 1 | 2 | 2 |
| `Reticle01` / `Reticle02` | 1024x1024 | DXT5 | 11 | 2 each | 2 each |
| `Vortex_Razor_UH-1_GenII_..._AlbedoTransparency` | 1024x1024 | DXT5 | 1 | 2 | 2 |
| `reticle` | 2048x2048 | DXT5 | 1 and 12 | 2 | 2 |
| `reticle2` | 2048x2048 | DXT5 | 12 | 2 | 2 |
| `Reticle` | 2048x2048 | DXT5 | 12 | 2 | 2 |
| `5-25x56 Reticle` | 4096x4096 | DXT5 | 13 | 1 | 1 (21.3 MB) |
| `chevon@4x` | 2048x2048 | DXT5 | 1 | 1 | 1 |
| `hamr_reticle_illuminated` | 2048x2048 | DXT5 | 1 | 1 | 1 |
| `Burris XTR 14 Reticle` | 1024x1024 | DXT5 | 1 | 1 | 1 |
| `reddot` | 1024x1024 | DXT5 | 1 | 1 | 1 |
| `reticle_jm1` | 2048x2048 | DXT5 | 1 | 1 | 1 |
| `Reticle4K` | 2048x2048 | DXT5 | 1 | 1 | 1 |
| `PSO RETICLE` | 1024x1024 | DXT5 | 11 | 1 | 1 |
| `Vortex HD Gen 3 Reticle` | 2048x2048 | DXT5 | 1 | 1 | 1 |
| `Scope_Leupold-Mark3HD-3-9x40_Reticle_Albedo` | 2048x2048 | DXT5 | 12 | 1 | 1 |
| `Scope_Leupold-Mark4-3.5-10x30-LR_T_Reticle_Albedo` | 2048x2048 | DXT5 | 12 | 1 | 1 |

The material column sums to 60. `5-25x56 Reticle` is the largest reticle in the build at 21.3 MB, though it does not stand alone: several 4096 DXT5 clothing normals in `charactermodmaterials` cost the same, and that is the game's ceiling.

`Reticle03` is the single highest-value asset in the game's optics: one 1000x1000 dot behind 15 materials covering the Aimpoint ACRO P2, PRO and Comp M4, the Trijicon RMR, the Trijicon SRS lens and the SIG Air reflex. `reticle_1dot` is the only uncompressed reticle in the build, so it has no BC artefacts to fight.

Several materials are named after the wrong optic. `holosun_reticle 1` and `holosun_reticle 2` are driven by `ROMEO8T_red_dot_reticle_Emission`, a SIG Romeo8T asset that also drives the correctly named `romoe 8t` pair; `bravo4_reticle 1` is driven by `hamr_reticle_illuminated`, a Leupold HAMR asset. `chevon@4x` is a shipped typo for chevron, and `Artboard 1` is a raw Illustrator export name. Search by bundle slug, never by texture name. The bundle `weaponmodmaterials_assets_razorhdg2e16_reticle_cf493bbe1aa96e2fc8966c8950f6f917.bundle` contains a reticle material with zero Texture2D objects; edit `razorhdg2e16_reticle1` instead.

### 7.9 Four shaders, two property vocabularies

Reticle materials sit on four shaders, and the render queue follows the shader, not the optic class. Queues below were read from `m_CustomRenderQueue` on the live bundles.

| Shader | Materials | Render queue | Reticle texture slot |
|---|---|---|---|
| `Ultimate Scope Shaders/HolographicSight` | 39, all bind a reticle | 3000 | `_Reticle` |
| `Shader Graphs/RealisticScopeEffect` | 18, of which 12 bind a reticle | 2000 | `Reticle` |
| `Shader Graphs/Reticle` | 8, all bind a reticle | 3000 | `Reticle` |
| `Ultimate Scope Shaders/Scope` | 1 (`Scope Material Base`, LPVO) | 2450 | `Reticle` |

`Shader Graphs/_NonDualRender RealisticScopeEffect` covers a further 5 materials, four of them ACOG render targets baked into `weaponmods_stripped` prefab bundles, none of which bind a reticle texture.

Family A, the magnified path on `RealisticScopeEffect`, `Shader Graphs/Reticle` and `Ultimate Scope Shaders/Scope`, uses unprefixed property names. Family B, the red dot and holographic path on `HolographicSight`, uses underscore-prefixed names. They are not interchangeable.

```
FAMILY A   Reticle (tex) / Reticle_Brightness / Reticle_Size / Reticle_Rotation
           Reticle_X_Offset / Reticle_Y_Offset / ReticleDepth / Use_First_Focal_Plane
           CameraZoom / Depth / EyeBox_Size / EyeBox_Softness
           Lens_Eye_Relief_Size / Lens_Eye_Relief_Softness / Lens_Shadow_Depth
           Override_Colour ;  tint is _Color

FAMILY B   _Reticle (tex) / _Reticle_Color (HDR) / _Reticle_Brightness
           _Retical_Size  <-- shipped typo, this is the real size knob
           _Reticle_Offset / _Reticle_Tiling / _NVG_Reticle / _Glass_Tint
           _USE_TEXTURE_COLOR
           blur   _BLURRETICLE + _Blur_Distance / _Blur_Range / _Blur_Samples
           noise  _USERADIALNOISE + _NumRadialSections / _RadialNoiseStrength /
                  _RadialNoiseRotationSpeed / _Radial_Noise_UV_Offset_Distance /
                  _Radial_Noise_UV_Offset_Speed
           offset _USE_OFFSET_NOISE + _Offset_Noise_Distance / _Offset_Noise_Scroll_Speed
```

Three traps follow. `_Retical_Size` is misspelled in the shipped shader, so spelling it correctly gives you a no-op. `_BLURRETICLE`, `_USERADIALNOISE`, `_USE_OFFSET_NOISE`, `_USE_QUAD_CLIP` and `_USE_TEXTURE_COLOR` are shader keywords as well as floats and must be present in `m_ValidKeywords`; setting the float without the keyword does nothing. And carrying both vocabularies is the norm, not the exception: 33 materials serialize both `_Retical_Size` and `Reticle_Size`, 32 of them on `HolographicSight` plus `burris reticle` on `Shader Graphs/Reticle`. The shader decides which set is live and the other is dead weight from a conversion. Read the property list and the shader name before you write.

Colour and brightness live on the material, not in the texture. Values are HDR over-range, and 2.9961 is the classic 3.0 encode:

| Material | `_Reticle_Color` or `_Color` | Brightness | `_Retical_Size` / `Reticle_Size` |
|---|---|---|---|
| `acrop2_reticle` | (2.9961, 0, 0, 1) | `_Reticle_Brightness` 0.5 | 13.3 |
| `acrop2_reticle nvg` | (2.9961, 2.9961, 2.9961, 1) | `_Reticle_Brightness` 100.0 | 13.3 |
| `rmr_reticle 2` | (5.9922, 0, 0, 1) | `_Reticle_Brightness` 1.0 | 10.0 |
| `aimpoint_pro_reticle 1` | (2.9961, 2.9961, 2.9961, 0.7216) | `_Reticle_Brightness` 1000.0 | 14.96 |
| `EXPS3_reticle 1` (`exps3_reticle11`) | (1.498, 0.0314, 0, 1) | `_Reticle_Brightness` 2560.0 | 100.0 |
| `EXPS3_reticle 1` (`exps3_reticle1`) | (1.498, 0.0314, 0, 1) | `_Reticle_Brightness` 1.0 | 100.0 |
| `UH1_reticle` | (145.3186, 0, 0, 1) | `_Reticle_Brightness` 1.0 | 28.1 |
| `pm2 shotdot Reticle` | (256, 256, 256, 1) | `_Reticle_Brightness` 1.0 | 15.0 |
| `acog_reticle` | `_Color` (1, 0, 0, 1) | `Reticle_Brightness` 215.0 | 50.0 |
| `525pm2_reticle` | `_Color` (0.9063, 0.9063, 0.9063, 1) | `Reticle_Brightness` 1.0 | 660.0 |
| `tango6t_reticle` | `_Color` (1, 0, 0, 1) | `Reticle_Brightness` 5.0 | 900.0 |

The two `EXPS3_reticle 1` rows are two different bundles carrying the same material name with brightness 2560 versus 1.0, which is a good reason to key on the bundle slug.

Author your reticle white, or as a pure alpha mask on transparent, and let `_Reticle_Color` tint it; baking red into the texture gives you red multiplied by red. Four materials set `_USE_TEXTURE_COLOR` to 1, but only three (`OKP7 Reticle`, `pm2 shotdot Reticle`, `Scope Material Base`) also carry the keyword in `m_ValidKeywords`. `burris reticle` sets the float without the keyword, which is the keyword trap shipped in the retail build. Only those three will carry a multi-colour texture through. Brightness has no shared scale between optics, so do not copy a value across: 1.0 is a normal dot on the S&B and invisible on the Aimpoint PRO.

### 7.10 NVG variants

Night-vision reticles are separate materials in separate bundles pointing at the same texture, differing only in properties. The flag is `_NVG_Reticle`, and it is 1.0 on 17 of the 39 `HolographicSight` materials. Only six of the seventeen have "nvg" in the material name; the rest of the pairs are distinguished by a trailing " 1" or " 2":

```
_NVG_Reticle = 1   material names, bundle slug in brackets where it differs

  named:     acrop2_reticle nvg      EXPS2 reticle nvg      OKP7 NVG Reticle
             EXPS3_reticle nvg (exps3_reticlenvg, exps3_reticlenvg1)
             hws_reticle nvg 2
  unnamed:   aimpoint_pro_reticle 1  rmr_reticle 1 (rmr_reticle11)
             rmr_reticle 5           holosun_reticle 2      mrs_reticle 2
             Reticle 2               Reticle CompM4 1       SRS Lens 1
             Lens 1                  UH1_reticle 1          romoe 8t 1
```

`rmr_reticle 2 nvg` carries `_NVG_Reticle = 0` despite its name, and expresses its NVG state only through `_Reticle_Color` (23.9686 white) and `_Reticle_Brightness` 10.59. Trust the flag, not the name. Measured day-to-NVG delta for the ACRO P2: `_NVG_Reticle` 0 to 1, `_Reticle_Color` (2.9961, 0, 0, 1) to (2.9961, 2.9961, 2.9961, 1), `_Glass_Tint` alpha 0 to 0.2157, `_Reticle_Brightness` 0.5 to 100.0.

The runtime side confirms two physical renderers rather than one switched material: `Il2Cpp.HWSReticleBrightness` exposes both `ReticleRenderer` and `ReticleRendererNVG`, and `Il2Cpp.ScopeInputSetter` carries `RegularScopeLayerMask`, `NVGScopeLayerMask`, an `NVGScopeObjectiveLens` transform and the methods `TrySetNVGCam()`, `EnableScopeNVG()` and `DisableScopeNVG()`. If you reskin `rmr_reticle2` and skip `rmr_reticle2nvg`, your reticle reverts to stock the moment the player drops nods.

### 7.11 How the reticle choice is persisted

Reticle selection is an integer index, not a texture reference. `Il2Cpp.ReticleSetting` is a serialisable data class with four members: `Name` (string), `ReticleBrightness` (float), `ReticleBrightness_NVG` (float) and `gfxObj_locRotation` (Vector3). `Il2Cpp.HWSReticleBrightness`, from `\Assets\Scripts\HWSReticleBrightness.cs`, holds `reticleSettings` as an `Il2CppReferenceArray<ReticleSetting>` plus `DefaultBrightnessSetting` and `CurrentBrightnessSetting` ints, and exposes `get_ReticleKey()`, `SetReticleSetting(bool alert)`, `OnMaterialLoadingDone()` and real `Start`/`Update` bodies, so it is directly Harmony-patchable.

`CurrentBrightnessSetting` is what gets saved, keyed by the runtime clone name. That the key comes from the clone name is inference from `get_ReticleKey()` plus the key format below; the literal `" reticle setting"` does not appear in the metadata string tables, so it is assembled at runtime. The store is plaintext JSON despite the extension:

```
%USERPROFILE%\AppData\LocalLow\VECTOR INTERACTIVE\OPERATOR\Saves\Save<N>\Loadouts\Version2\<slot>.es3
```

A live slot file confirms the shape, 61 keys of which 42 are reticle settings:

```json
"Trijicon RMR(Clone) reticle setting"           : { "__type": "int", "value": 5 },
"Aimpoint ACRO P2 FDE(Clone) reticle setting"   : { "__type": "int", "value": 6 },
"EOTECH EXPS-3 FDE(Clone) reticle setting"      : { "__type": "int", "value": 13 },
"OKP7(Clone) reticle setting"                   : { "__type": "int", "value": 0 },
"Vortex Razor AMG UH-1 Gen II(Clone) reticle setting" : { "__type": "int", "value": 0 }
```

Keys exist for mounts as well as optics (`Aimpoint Mount(Clone)`, `RMR Mount(Clone)`), so the component is attached more widely than the reticle-material list suggests. Two consequences for modders. Renaming or re-instantiating an optic prefab orphans the player's saved choice. And `gfxObj_locRotation` means a reticle setting also rotates a graphics object, presumably the brightness dial on the optic body, so a body reskin that moves that dial mesh invalidates the saved rotations.

`OnMaterialLoadingDone()` proves optic materials load asynchronously. Any mod that writes a reticle texture or material property must sequence after it fires, or the async load overwrites the write.

### 7.12 The scope render path

There is no single path. Magnified optics render through a real second camera, red dots are a screen-space effect, and both feed HDRP CustomPasses. The tree below is assembled from IL2CPP type and method signatures; every identifier in it is present in the metadata, but the call ordering is inference from the names.

```
ADS on a magnified optic
 `- Il2Cpp.ScopeInputSetter   (per-optic glue, real Update, \Assets\Scripts\milk_drinker\Scopes\)
     |- Magnification, ZoomMode, targetZoom/currentZoom, ZoomSpeed, ZoomSens
     |- scopeZero -> Il2Cpp.ScopeZero  (doesZero, ClicksAdjustment, moveReticle)
     |- reticleBrightness -> Il2Cpp.ReticleBrightness (v_brightness_min/max, keycodes)
     |- RegularScopeLayerMask / NVGScopeLayerMask / NVGScopeObjectiveLens
     |- SetLowRes() / SetHighRes() / ApplyRenderInterval() / InitFailsafeShit()
     `- ScopeCamera -> Il2CppUltimateScopes.ScopeController
          |- DualRenderCamera (Camera) --renders--> RenderTexture
          |    `- SetOutputRenderTexture(RenderTexture)
          |- LensRenderer (Renderer), ObjectiveLensPosition (Transform)
          |- LateUpdate -> SetShaderConfigValues()
          |- cached ids: ExitPupilRadiusID, EyeReliefID, SceneFOVID, ReficleFOVID(typo),
          |              MagnificationID, BrightnessID, ErectorDistortionID, EyeDirOSID
          |- throttles: RenderInterval, RenderSkip, FixedRenderFPS, MaxRenderFPS
          `- ScopeSpecification -> Il2CppUltimateScopes.ScopeSpecification
               |- ReticleTexture (Texture2D)   <-- the authoring knob
               |- ReticleScale (float)
               |- MagnificationMin / MagnificationMax / IsFFP
               |- GetExitPupilRadius(mag) / GetEyeReliefMeters(mag) / GetErectorDistortion(mag)
               `- GetObjectiveDistortion / GetEyepieceDistortion / DisableScopeShadow

HDRP CustomPass chain (parallel, screen space)
 |- Il2Cpp.ScopeDOF   (magnified)  Setup / Execute(CustomPassContext) / Cleanup
 |    |- AggregateCullingParameters override, GenerateLensMask, CombineMask,
 |    |  GetGaussianWeights -> ComputeBuffer, WeightedRadialBlur, WeightedGaussianBlur
 |    `- lensMaskBuffer, combinedMaskBuffer, QuadClipMaterial, whiteRenderersMaterial
 `- Il2Cpp.RedDotDOF  (red dots; same minus the mask and quad-clip path)

NVG integration
 `- Il2Cpp.NvgScopeFollower : static s_scopeCount, IsScope override
```

`ScopeSpecification.ReticleTexture` plus `ReticleScale` is the authoring-level knob for magnified optics, and setting it should replace the reticle without touching a bundle or a material. `ScopeController.SetOutputRenderTexture(RenderTexture)` lets you grab or replace the scope image itself, which is the seam for a thermal overlay or a digitally drawn reticle. Neither has been exercised at runtime here.

Two performance traps carry over. `ScopeDOF.AggregateCullingParameters` is overridden, so the pass costs culling time even at `EffectStrength` 0; disable the CustomPassVolume rather than zeroing the strength. And any graphics-preset mod that iterates `CameraSettings.AllCameraSettings` must skip entries where `isScopeCamera` is set, or optics break.

`Il2Cpp.RealisticScopeEffectV2`, from the bought asset at `\Assets\_tools\VectorAssets\Realistic Scope Effect V2\`, is a near-duplicate of `ScopeInputSetter` down to sharing the method `InitFailsafeShit()`. Only `ScopeInputSetter` references `ScopeController`, and the metadata still holds an identifier `realisticScope`, so one of the two is probably dead but which one is unverified and needs a runtime probe. Do not guess; equip a magnified optic and dump every component on the prefab hierarchy. If RSE V2 turns out to be live, its `AssignMaterial` to `ActuallyAssignMaterial` to `AfterAssignMaterial` chain is the correct hook point.

### 7.13 Procedure: shipping a custom reticle

Path A, bundle replacement, no code:

1. Find the optic's reticle bundle by slug in the live folder. Never work from a saved filename; the hash is a content hash and a bulk patch rewrote 1,108 bundles in a single day this year.

   ```powershell
   Get-ChildItem "D:\Games\OPERATOR\OPERATOR_Data\StreamingAssets\aa\StandaloneWindows64" `
     -Filter "weaponmodmaterials_assets_rmr_reticle2_*.bundle"
   ```

2. Open it with `UnityPy.config.FALLBACK_UNITY_VERSION = "6000.3.8f1"` and read the material's texture-env list. `_Reticle` present means the red-dot vocabulary; `Reticle` present means the magnified vocabulary. Confirm the shader through `m_Shader.read().m_ParsedForm.m_Name`, and remember 33 materials carry both vocabularies, so the shader name is the tiebreak. Some materials' shader PPtr points into an external shared CAB and raises `FileNotFoundError` when read from a single bundle; guard the call.

3. Author the replacement at the same width, height, format and mip count as the existing texture, which ranges from 512x512 RGBA32 to 4096x4096 DXT5 with 13 mips. Paint white on transparent unless `_USE_TEXTURE_COLOR` is on for that material and the keyword is in `m_ValidKeywords`.

4. Swap the image and repack. Verified round trip on `rmr_reticle2`:

   ```python
   import UnityPy
   from PIL import Image
   UnityPy.config.FALLBACK_UNITY_VERSION = "6000.3.8f1"
   env = UnityPy.load(SRC)
   for obj in env.objects:
       if obj.type.name != "Texture2D":
           continue
       tex = obj.read()
       if tex.m_Name != "Reticle03":
           continue
       tex.set_image(Image.open("my_reticle.png").convert("RGBA"),
                     target_format=int(tex.m_TextureFormat),
                     mipmap_count=tex.m_MipCount)
       tex.save()
   open(OUT, "wb").write(env.file.save(packer="lz4"))
   ```

   Keep the Texture2D name and the Material name unchanged. Editing the existing material beats authoring a new one, because a rebuilt material needs a shader you cannot obtain from the stripped player build.

5. If the optic has an NVG twin, repeat there. Check `_NVG_Reticle` rather than the bundle name to find it.

6. Drop the repacked bundle in under the current live filename. No content-hash check is performed.

Path B is a runtime MelonMod, and it survives game patches because there is no filename hash to chase. Prefer appending to replacing: `Il2Cpp.ReticleCycling` exposes `reticleCycling` (bool), `reticleIndex` (int) and `allReticles` as `List<UnityEngine.Texture2D>`, so `GetComponentInChildren<ReticleCycling>().allReticles.Add(myTexture)` should put a custom reticle into the vanilla cycle key alongside the stock set, though which optics actually carry the component is unverified and needs a component dump on each equipped optic. For magnified optics, write `ScopeSpecification.ReticleTexture` and `ReticleScale` instead. Failing both, write the material properties directly, respecting the family split from 7.9 and enabling the matching keyword for any `_USE*` float you set. Whichever route you take, sequence every write after `HWSReticleBrightness.OnMaterialLoadingDone()` fires, because optic materials load asynchronously and a late load overwrites an early write.

Highest payoff per unit effort, in order: `Reticle03` in the `rmr_reticle*` bundles, since one texture backs 15 of the 60 reticle materials; `reticle_1dot` in the EOTech bundles, uncompressed so no BC artefacts; `chevon@4x` in `acog_reticle`, the ACOG chevron; and `5-25x56 Reticle` in `525pm2_reticle`, a 4096 sheet with room for real mil-hash subtension detail.

## 8. World, effects and interface art

Start with the arithmetic, because it settles the question before you open a single file. The four art families account for 4,974 of the 4,989 live bundles. Grouping every bundle in `objects.tsv` by the segment before `_assets_` leaves exactly fifteen others:

```
weaponmods_stripped         1581   prefabs
weaponmodmaterials          1203   materials
charactermods_stripped      1165   prefabs
charactermodmaterials       1025   materials
                            ----
                            4974
localization-*                12   120 MonoBehaviour total, no art
<guid>_monoscripts             1   69 MonoScript, 5,409 B
<guid>_unitybuiltinassets      1   1 Material "Sprites-Default" + 1 shader stub, 19,433 B
defaultlocalgroup              1   mat_carryhandle, 9,240,388 B
                            ----
                            4989
```

The folder also holds one non-bundle file, `Patch_Select_Russia.jpg`, an orphan build artifact.

Everything that is not a character mod or a weapon mod is a rounding error. No bundle carries terrain, vegetation, skybox, icon or sprite-atlas art. The world, the effects and the interface are baked into `.assets` and `levelN` files that Addressables never touches, and the proven bundle-replacement workflow from section 6 reaches almost none of it. Plan accordingly: a gear reskin is a bundle project, a world or UI reskin is file surgery on the player data or a MelonLoader runtime job.

### 8.1 Where the art actually lives

Across `globalgamemanagers.assets`, `resources.assets` and the 31 `sharedassetsN.assets` files there are 11,614 Texture2D, 3,370 Material, 1,087 Cubemap, 47 TerrainLayer and 3,435 Sprite objects. The addressable set holds 7,247 Texture2D and 2,603 Material in 4,989 bundles. The baked side is the larger half of the game's art by object count and by weight.

Weight is where the published `.assets` sizes mislead, so get this straight before you plan an edit. An `.assets` file holds serialized objects only; the pixels live in the paired `.assets.resS` and `.resource` sidecars. The 33 `.assets` files total 6,064 MB, their sidecars add 31,973 MB, and the 31 `levelN` files add 687 MB, so the baked side is 38.7 GB against 13.2 GB of bundles. Every offline texture edit rewrites the sidecar as well as the `.assets` file.

The `levelN` files carry none of the art. A type census over `level0` through `level30` returns 2 Material, 0 Texture2D and 0 Sprite, against 31 Terrain components and 120,348 Tree instances. A level file is an object graph; its art sits in `sharedassetsN.assets`. Edit the level file to move things, edit the shared file to repaint them.

| File | `.assets` | With sidecars | Texture2D | Material | Sprite | Cubemap | Role |
|---|---:|---:|---:|---:|---:|---:|---|
| `globalgamemanagers.assets` | 255.1 MB | 312.9 MB | 132 | 38 | 1 | 1 | 351 shaders, HDRP defaults, the preloaded impact VFX set |
| `resources.assets` | 18.5 MB | 78.9 MB | 624 | 70 | 503 | 0 | always-resident UI icon library and TMP atlases |
| `sharedassets0.assets` | 1,869.8 MB | 5,867.7 MB | 1,695 | 691 | 24 | 3 | global gear/weapon art, audio, branding |
| `sharedassets1.assets` | 474.1 MB | 2,999.0 MB | 3,382 | 293 | 2,846 | 2 | menu and customisation scene, the inventory icon store |
| `sharedassets2.assets` | 88.2 MB | 1,709.7 MB | 783 | 328 | 59 | 7 | hub / Operation Room, laptop UI |
| `sharedassets3.assets` … `sharedassets30.assets` | 0.2-598.9 MB | 2.7-2,716.4 MB | 2-674 | 0-243 | 0-2 | 1-178 | map art, keyed to a `levelN` but cross-referenced widely |

That last row is keyed, not owned. Read a level file's external list and you will see fifteen to twenty-five shared files, so a texture you replace in one map's file can surface in another map.

`resources.assets` is the easiest target in the game: 18.5 MB of objects, 78.9 MB including sidecars, 624 textures. `sharedassets23`, `24`, `25`, `28` and `29` are all under 1.1 MB of objects and hold almost nothing but baked probes, as are `sharedassets5` and `sharedassets10`. `sharedassets0` at 5.9 GB with sidecars is the one you do not want to rewrite casually.

### 8.2 The addressable slice of effects art

Twelve bundles outside the gear tree carry effects materials, all under `weaponmodmaterials`. This is the entire moddable effects surface reachable by the safe file-replacement route. The nine that carry a texture are four-object bundles holding one Texture2D, one Material, one shader stub and one AssetBundle, DXT5 throughout. The three laser bundles carry no Texture2D at all.

| Bundle slug and current hash | Bytes | Material | Shader | Texture bound to `_MainTex` |
|---|---:|---|---|---|
| `muzzleflash1_d8b22082e247dc96bf10d41d6fdf2439` | 45,488 | `MuzzleFlash1` | `KriptoFX/FPS_Pack/Particles` | `MuzzleFlash1` 256×256, 9 mips |
| `muzzleflash2_2acdc4b647125977a8e69b7547115d26` | 152,754 | `MuzzleFlash2` | `KriptoFX/FPS_Pack/Particles` | `Flame1` 512×512, 10 mips |
| `muzzleflash4_2618df77c5073693d1d940f242aa5996` | 70,274 | `MuzzleFlash4` | `KriptoFX/FPS_Pack/Particles` | `MuzzleFlash3` 256×256, 9 mips |
| `muzzleflash5_e6c70455601c501334283fbe2305d137` | 44,844 | `MuzzleFlash5` | `KriptoFX/FPS_Pack/Particles` | `MuzzleFlash4` 256×128, 9 mips |
| `flame1_5fb410f51dc889ec440b72443ed2fdc2` | 152,760 | `Flame1` | `KriptoFX/FPS_Pack/Particles` | `Flame1` 512×512, 10 mips |
| `flame2_de3cf4b49e764ebe50623e230259a91a` | 198,032 | `Flame2` | `KriptoFX/FPS_Pack/Particles` | `Flame2` 1024×512, 11 mips |
| `muzzleparticles_b3c01d83e97c2aefafb6a16a8dc05569` | 20,038 | `MuzzleParticles` | `KriptoFX/FPS_Pack/GlowAdditiveNoFade` | `Particle` 32×32, 1 mip |
| `smoke_fe5bfb56ec39ed0a041d1a6882d93a89` | 467,591 | `Smoke` | `KriptoFX/FPS_Pack/AlphaBlendedAnim` | `Smoke` 2048×2048, 12 mips |
| `distortion_eb6dc9101aba97c081f9d323ee1703ab` | 79,334 | `Distortion` | `KriptoFX/FPS_Pack/Distortion` | `Distortion2` 256×256, 9 mips |
| `irlaser_9a902881fbe669a9e30ac253ef09163f` | 34,474 | `IR Laser` | `Shader Graphs/IR Laser` | none, emissive only |
| `vislaser_6249d2d61ea1ee00960952f26873d2c4` | 27,102 | `Vis Laser` | `HDRP/Unlit` | none, emissive only |
| `vislasergreen_eb848d3961974fc705ef128c03a7f770` | 427,634 | `Vis Laser Green` | `HDRP/Lit` | none, emissive only |

Three traps here. The texture name is offset from the material name, so `MuzzleFlash4.mat` uses a texture called `MuzzleFlash3` and `MuzzleFlash2.mat` uses `Flame1`; search by bundle slug, never by texture name. There is no `muzzleflash3` bundle at all, so the 1, 2, 4, 5 numbering has a real hole and a missing file is not a bad scan. And `Flame1` ships identically in two bundles, `flame1` and `muzzleflash2`, verified by decoding both to pixels and comparing hashes; this is the same Addressables duplication behaviour documented for mask and normal maps, so repaint one and the other still shows the stock art.

The three laser materials have no texture and carry their whole appearance in float4 properties, and they are not on one shader, which matters if you plan to rebind. `IR Laser` ships `_EmissiveColor = (732.33, 1248.02, 1124.05, 58.84)` with `_EmissiveIntensity = 58.844` and `_EmissiveColorLDR = (0.4223, 0.8803, 0.0000, 1.0000)`. `Vis Laser` ships `_BaseColor = (1.0000, 0.1765, 0.0000, 0.0078)` and `_EmissiveColor = (11.98, 0.5670, 0.0000, 1.0000)`. `Vis Laser Green` ships `_BaseColor = (0.0000, 1.0000, 0.0000, 0.0863)` and `_EmissiveColor = (8.8665, 766.9960, 0.0000, 1.0000)`. Changing laser colour is a property edit, not a texture edit.

Two other pieces of non-gear art ride inside the addressable set. The only HDRP/Decal materials in any bundle are the Mk18 rollmarks: `weaponmods_stripped_assets_danieldefencemk18upperreciever_7bfeb63798b0ce2fb0385a83e382917e.bundle` and its FDE twin each hold materials `Decal` and `Decal 1` binding `5556` (1024×1024 DXT5, 11 mips) and `MK18` (512×512 DXT5, 10 mips) through `_BaseColorMap`. Bullet holes, blood and scorch decals are not among them. Separately, two bundles ship the gore tiling set on `GoreShader` through the non-standard slots `_MeatBaseMap` and `_MeatNormalMap`, pointing at `meat_tile_AlbedoTransparency_1` (2048×2048 DXT1) and `meat_tile_Normal` (2048×2048 DXT5), in `charactermodmaterials_assets_m_head_b680b359fc074c55b1cc7f4e4af3e0a2.bundle` and `charactermods_stripped_assets_camoc_83e64de0d9f9f0a66cd03c2e0adb3b07.bundle`.

Fonts are almost entirely baked, with two exceptions. `charactermodmaterials_assets_amarurgtsdf_ea6620344e0ffbe8f7819da4e8f09b30.bundle` (223,442 B) holds `amarurgt SDF Material` on `TextMeshPro/Distance Field` with `amarurgt SDF Atlas` at 1024×1024 Alpha8, one mip; the catalog places it under the `_ui/_fonts` group and the naming implies it is the localisation fallback face, which a runtime probe would confirm. `charactermods_stripped_assets_garmingps_c70169c424266af16b8e9110c08b2ea8.bundle` (534,879 B) carries `LiberationSans SDF Material` on `TextMeshPro/Mobile/Distance Field` with a 1024×1024 Alpha8 atlas, which is Unity's stock TMP font dragged in with the Garmin GPS screen prefab.

### 8.3 Impact effects, decals and blood are preloaded, not scene-local

The banked note in this area assumed impact and decal VFX were pooled prefabs sitting inside map scenes. They are not. A census of `globalgamemanagers.assets` puts them in the always-resident preload table: 111 GameObject, 73 ParticleSystem, 73 ParticleSystemRenderer, 38 Material, 132 Texture2D, 11 Mesh, 1 Cubemap. The GameObject names are the per-surface impact prefabs, and they are unambiguous.

```
globalgamemanagers.assets  (255.1 MB objects, 312.9 MB with sidecars)
├── impact prefabs   BrickImpact ×2   ConcreteImpact ×2  DirtImpact ×2
│                    GlassImpact ×2   GrassImpact ×1     MetalImpact ×4
│                    PlasterImpact ×2 RockImpact ×2      WoodImpact ×2
│                    WaterImpact ×2   Foliage ×2
├── decal objects    Decal Projector ×13, Decal ×4
├── particle children Blood 1 ×1, Blood 2 ×1, Dust ×15, DustVertical ×11,
│                    GlassParticles ×4, Leafs ×2, Leafs (1) ×2,
│                    MetalImpact (1) ×2, Smoke ×2, WaterParticles ×2,
│                    WaterParticles2 ×2, WaterRings ×2,
│                    WoodChips ×4, rocks ×14, rocks (1) ×7
├── HDRP/Decal mats  BrickBulletDecal, ConcreteBulletDecal, DirtBulletDecal,
│                    MetalBulletDecal, WoodBulletDecal
├── decal textures   BrickBulletDecal3 256²  DirtBulletDecal2 128²
│                    DirtBulletDecal3 256²   MetalBulletDecal 128²
│                    WoodDecal2 128²         BrokenGlassDecal 512²   (all DXT5, 1 mip)
├── blood            mats Blood Particles 1 / 2 (HDRP/Lit)
│                    tex  Blood 1 512² DXT5 10 mips, Blood 2 128² DXT5 8 mips
├── debris mats      BrickDust, BrickDust 1, BrickRocks, ConcreteDust,
│                    ConcreteDustVertical, DirtDust, DirtRocks, PlasterDust,
│                    PlasterDust2, PlasterRocks, RockDust, RockDust2,
│                    WoodChip, WoodDust2, MetalSmoke, Leaf, Rock, Stone, GlassDecal
├── KriptoFX mats    Glass (…/Glass), MetalParticles (…/Particles),
│                    WaterImpact, WaterImpact 1, WaterImpactRing (…/WaterParticles)
└── sky and water    PhysicallyBasedSky (HDRP/PhysicallyBasedSky),
                     Water, Water Decal (HDRP/Water/*), MaterialWaterExclusion,
                     DefaultHDRISky cubemap 128 BC6H
```

Those 38 materials are the complete list, with the two HDRP/Unlit area-light viewers and `Sprites-Default` making up the remainder. Operationally this is good: the whole per-surface impact set is one file, it is loaded before any scene, and 12 of its 139 MonoBehaviours resolve through their MonoScript to `FPS_UseLight`, the KriptoFX demo light script shipped into release. The other 127 do not deserialise without a typetree. A runtime mod does not have to wait for a map load to find these objects, and the surface set is small enough to enumerate by name.

The second effects system is Unity VFX Graph, and it is a hard wall. There are 22 `VisualEffectAsset` objects and 130 `VisualEffect` components across the build, against 798 `ParticleSystem`. The graphs are named `Frag explosion`, `S-VEST explosion`, `Breaching charge`, `Glass hit`, `m320 explosion 1`, `Metal hit`, `Concrete hit vector edit 2`, `Flashbang pop` (all in `sharedassets0`), `Sparks`, `m72 law firing`, `m72 law backblast`, `IED explosion` (`sharedassets1`), `FloatingDust` (`sharedassets2`), `water leaking` (`sharedassets9`), `Fire smoke`, `Smoke grenade`, `Smoke effect 1`, `Dust kickup`, `ttest`, `Smoke effect big` (`sharedassets20`), and `Cockroach_VFX_01` / `Cockroach_VFX_02` (`sharedassets30`). `ttest` is a leftover test graph that shipped.

Those graphs compile to their own shaders, and this reframes the shader registry. `globalgamemanagers.assets` holds 351 Shader objects of which 214 are `Hidden/*`, but 100 of those 214 are `Hidden/VFX/<graph>/<system>/Output …` entries. Only 114 are engine and HDRP internals. Fifteen graph names appear in those shader paths, so seven of the 22 graphs contributed no compiled output to this file. You can read a graph's system layout straight off the shader names, for example `Hidden/VFX/IED explosion/Shockwave/Output Particle HDRP Distortion Mesh`, and you can swap the textures those outputs sample, but the graph's simulation logic is compiled and there is no editing it without the Unity project.

### 8.4 Terrain, vegetation and props

There are 31 Terrain components across the 31 level files and 47 TerrainLayer objects. TerrainData count is 20, of which 19 sit in shared files paired with their level and one, `MapMagicDefaultTerrainData` in `resources.assets`, is a dead procedural-tool leftover with zero layers. No TerrainData ships in a bundle.

| Shared file | TerrainData | Layers | Alphamap res | Basemap res | Heightmap res |
|---|---|---:|---:|---:|---:|
| `sharedassets3` | `New Terrain 31` | 2 | 2048 | 2048 | 2049 |
| `sharedassets4` | `CleanHouse_Terrain` | 1 | 1024 | 1024 | 1025 |
| `sharedassets6` | `New Terrain` | 2 | 512 | 1024 | 513 |
| `sharedassets8` | `New Terrain 3` | 5 | 1024 | 1024 | 2049 |
| `sharedassets11` | `New Terrain 6` | 2 | 1024 | 2048 | 513 |
| `sharedassets12` | `New Terrain 8` | 7 | 2048 | 2048 | 2049 |
| `sharedassets13` | `Nuke_Town` | 5 | 2048 | 2048 | 1025 |
| `sharedassets14` | `New Terrain 9` | 2 | 2048 | 2048 | 2049 |
| `sharedassets16` | `New Terrain 5` | 6 | 2048 | 2048 | 2049 |
| `sharedassets18` | `New Terrain 1 1` | 7 | 2048 | 2048 | 2049 |
| `sharedassets19` | `New Terrain 20` + 3 GUID-named | 7 each | 2048 | 2048 | 1025 |
| `sharedassets20` | `New Terrain` | 5 | 2048 | 2048 | 2049 |
| `sharedassets21` | `New Terrain 3` | 5 | 2048 | 2048 | 2049 |
| `sharedassets22` | `New Terrain 1` | 8 | 2048 | 2048 | 2049 |
| `sharedassets26` | `New Terrain 2` | 8 | 2048 | 2048 | 2049 |
| `sharedassets27` | `New Terrain 3` | 7 | 2048 | 2048 | 2049 |

Two TerrainData objects carry a human name, `Nuke_Town` and `CleanHouse_Terrain`. `New Terrain 3` is used three times, in `sharedassets8`, `21` and `27`, and `New Terrain` twice, so name is not a key; use the owning file. `sharedassets19` holds three named `TerrainData_<uuid>`; the naming implies tool-generated or runtime-generated terrain rather than hand-authored, which is inference from the name alone. Layer counts exceed the local TerrainLayer count in most files because layers are shared by PPtr across the build.

The retexture seam is TerrainLayer, and it is a plain typetree object you can read and rewrite:

```
TerrainLayer "TL_Burnt_Sand_01"          (sharedassets12.assets)
  m_DiffuseTexture     {m_FileID:0, m_PathID:49}
  m_NormalMapTexture   {m_FileID:0, m_PathID:65}
  m_MaskMapTexture     {m_FileID:0, m_PathID:83}
  m_TileSize           (4.0, 4.0)
  m_DiffuseRemapMin    (0, 0, 0.039, 0)
  m_DiffuseRemapMax    (1, 1, 1, 15.91)
  m_MaskMapRemapMin    (0, 0, -0.34, 0)
  m_MaskMapRemapMax    (1, 1, 0.66, 1)
  m_SmoothnessSource   1
```

The PPtrs are `m_FileID: 0`, so the three textures sit in the same shared file at the given PathIDs. Replace those Texture2D objects with UABEA or AssetsTools.NET and the terrain repaints. The remap fields matter as much as the pixels; `m_DiffuseRemapMax.w = 15.91` means the layer is being pushed hard and a naive replacement will read wrong even at correct resolution.

Terrain shading is thinner than the shader registry suggests. Across all 3,332 baked materials, `InTerra/HDRP Tessellation/Terrain (Lit with Features) 2023.1 or Heigher` accounts for 16, a second variant `InTerra/HDRP/Terrain (Lit with Features) 2023.1 or Heigher` for 1, and stock `HDRP/TerrainLit` for exactly 1. Note the shipped typo in both InTerra names; pass them to `Shader.Find` spelled wrong or you get null.

Vegetation and most props are the single biggest trap in this area, and it is a trap of absence. The IL2CPP metadata carries `Il2CppBRGInstancedRenderer` with `BRGRenderer`, `BRGRegisterer`, `PrototypeData`, `InstanceData`, `BRGMeshLodData` and `RangeAllocator`, so this art is drawn through `BatchRendererGroup`. Those objects have no GameObject and no Renderer, which means `FindObjectsOfType`, renderer raycasts and `renderer.material` writes all miss them and your patch will look like it never bound. The type inventory is proven from metadata; that the material lives on the BRG prototype rather than any scene object follows from the API and is inference until you dump `PrototypeData` at runtime. Terrain-painted trees are the exception and do exist as real `Tree` instances: `level16` and `level17` carry exactly 60,174 each, which is the entire 120,348 build total.

The world-material shader census tells you where the art actually is. Counting every material in `resources.assets` and `sharedassets0` through `sharedassets30` and resolving each `m_Shader` PPtr gives 3,332 materials, of which 3,318 resolve to 268 distinct shader names. The remaining 14 do not resolve: 11 are legacy `Font Material` objects pointing into Unity's default resources, and 3 carry a null shader PPtr.

| Shader | Materials |
|---|---:|
| `HDRP/Lit` | 1784 |
| `MilkShaders/Lit-Template` | 365 |
| `MilkShaders/UERemap` | 141 |
| `HDRP/Decal` | 102 |
| `Shader Graphs/BotD_Graph_Lit_TranslucentAlphaCutoff` | 71 |
| `HDRP/Autodesk Interactive/AutodeskInteractive` | 54 |
| `Shader Graphs/S_PBR_OPAQUE_ORM_LeartesMasterMaterial` | 54 |
| `Shader Graphs/BotD_Graph_Lit` | 49 |
| `M_BlendMaster` | 45 |
| `TextMeshPro/Distance Field` | 44 |
| `HDRP/Unlit` | 39 |
| `Shader Graphs/MainShader` | 33 |
| `MilkShaders/Lit-Template-AddLayer` | 20 |
| `Shader Graphs/Lit Octahedral Impostor` | 17 |
| `InTerra/HDRP Tessellation/Terrain (…)` | 16 |
| `DBK HDRP/*` (all nine variants) | 10 |
| `SpeedTree/HDRP/Nature/SpeedTree9_HDRPCUSTOM` | 2 |

Two corrections to expectations here. The 102 baked `HDRP/Decal` materials against 4 addressable ones is where all the projected art in this game lives. And `DBK HDRP/*` totals 10 materials across the whole build despite shipping nine shader variants, so it is not the architecture backbone the shader list implies. MilkShaders is the backbone, at 578 materials in total, of which the `Lit-Template` family accounts for 412 and `UERemap` for 141 on Unreal-sourced ORM assets.

### 8.5 Sky, reflections and the rebake wall

The build holds 1,087 Cubemap objects. Nineteen of them are authored, and the other 1,068 are named `ReflectionProbe-N`, against 1,143 ReflectionProbe components across the level files.

| Name | Where | Size | Format |
|---|---|---:|---|
| `DefaultHDRISky` | `globalgamemanagers.assets` | 128 | BC6H |
| `starmap_2020_4k` | `sharedassets0` | 2048 | BC6H |
| `Simple_Cubemap` | `sharedassets0` | 1024 | BC6H |
| `modern` | `sharedassets4` | 512 | DXT1 |
| `Interior_A_Day 1`, `Interior_A_Night` | `sharedassets4` | 1024 | DXT1 |
| `CeilingFan_Cookie` | `sharedassets22` | 128 | DXT5 |
| 12 × `*-Cube-IES` light cookies | `sharedassets0/1/2/3/4/5/19/23/25/27` | 128 | RGB9e5Float |

A sky reskin is tractable and high-impact: `DefaultHDRISky` in `globalgamemanagers.assets` is global, `starmap_2020_4k` covers night maps. The wall is everything downstream. The 1,068 probe cubemaps are baked, and so is `StreamingAssets/APVStreamingAssets`, which holds 26 GUID-named Adaptive Probe Volume baking sets of four `.bytes` files each, 104 files totalling 2.37 GB. Baked lightmaps sit in the shared files too; `sharedassets2` alone carries `Lightmap-0` through `Lightmap-4`, each a 2048² BC7 `_comp_dir` plus a 2048² BC6H `_comp_light`. There is no way to rebake any of this without the Unity project. Any terrain or architecture retexture leaves reflections and bounce lighting referencing the old albedo, so plan changes that hold overall luminance or the mismatch shows on every wall.

### 8.6 Day and night variants share art, not lighting

Six maps ship as a day/night or variant pair, visible as near-identical object counts in the level files. The paired shared files are not symmetric, and this changes the work.

| Pair | Art-carrying file | `.assets` | Textures | Twin | `.assets` | Textures | Cubemaps |
|---|---|---:|---:|---|---:|---:|---:|
| 16 / 17 | `sharedassets16` | 245.5 MB | 165 | `sharedassets17` | 24.4 MB | 14 | 2 / 2 |
| 19 / 25 | `sharedassets19` | 367.7 MB | 626 | `sharedassets25` | 0.7 MB | 2 | 64 / 59 |
| 20 / 24 | `sharedassets20` | 598.9 MB | 339 | `sharedassets24` | 0.5 MB | 2 | 178 / 157 |
| 21 / 23 | `sharedassets21` | 160.2 MB | 176 | `sharedassets23` | 1.0 MB | 2 | 15 / 16 |
| 22 / 29 | `sharedassets22` | 220.2 MB | 285 | `sharedassets29` | 0.9 MB | 4 | 139 / 138 |
| 27 / 28 | `sharedassets27` | 83.3 MB | 212 | `sharedassets28` | 0.5 MB | 5 | 49 / 49 |

All but five of those cubemaps are `ReflectionProbe-N`; the exceptions are one IES cookie each in `sharedassets19`, `23`, `25` and `27`, and `CeilingFan_Cookie` in `sharedassets22`.

The sharing is proven, not assumed: each twin level file lists the heavy file in its externals, so `level17` references `sharedassets16`, `level25` references `sharedassets19`, `level24` references `sharedassets20`, `level23` references `sharedassets21`, `level29` references `sharedassets22` and `level28` references `sharedassets27`. One texture edit in the heavy file therefore covers both times of day, which is the opposite of what you would assume from the level files. The baked lighting is separate and will not follow your edit.

### 8.7 The interface layer

UI art splits across three files with three different jobs.

`resources.assets` is the always-resident icon library, 503 Sprite and 624 Texture2D drawn from only 350 distinct texture names. The source is the Michsky Modern UI Pack and DreamOS libraries, confirmed by the shipped source paths `\Assets\_ui\Modern UI Pack\Scripts\Icon\IconLibrary.cs` and `\Assets\_ui\DreamOS - Complete OS UI\Scripts\Icon Library\IconLibrary.cs`. 122 of the 350 names appear more than once. 53 appear exactly three times, of which 52 are the exact 64×64 / 128×128 / 256×256 triple. Another 40 appear four times, and those split two ways: 24 are the triple with a duplicated 128×128, and 16 are the triple plus a 32×32. Replace all copies of a name or your art shows up at one scale only, which reads in game as an icon that changes when a panel resizes.

Fonts in this file are ten TMP atlases and nine legacy Font objects. `AprilSans-Bold`, `AprilSans-Light`, `Larke Sans Bold`, `Larke Sans Light`, `Larke Sans Regular`, `OpenSans-Thin` and `Roboto-Regular` ship 512×512 Alpha8 atlases. `Anton SDF Atlas`, `Bangers SDF Atlas` and `Oswald Bold SDF Atlas` are 1×1, which means dynamic mode: the atlas regenerates at runtime and replacing the texture offline does nothing. To change those you must replace the TMP_FontAsset's source font. The legacy Font objects are `OpenSans-Bold`, `OpenSans-ExtraBold`, `OpenSans-Regular`, `OpenSans-Semibold`, `OpenSans-SemiboldItalic`, `Inter-Regular`, `Roboto-Regular`, `PerfectDOSVGA437` and `aline_font`.

`sharedassets0.assets` holds exactly 24 Sprites, and they are the branding and HUD set: `OPERATOR Logo Trident Near Final 1`, `New Operator Logo`, `New Operator Logo Background`, `Placeholder Logo`, `splashscreen 2`, `splashscreen 3`, `store_page_background`, `banner`, `banner 2`, `box`, `Background`, `Background Basic`, `Square Filled`, `Sand Clock`, `Sand Clock Sand`, `Cut Frame - 3px`, `Cut Frame Filled`, `Cut Frame Glow - 3px`, `9 Bang icon`, `flashbang icon`, `m67 icon`, `slap charge icon`, `helo_fin`, `renr_fin`. `Placeholder Logo` shipped alongside the finals.

`sharedassets1.assets` is the inventory icon store and the deploy-screen art, covered next.

One shader note that will cost you an hour if you miss it: `Shader.Find("Sprites/Default")` can return null, because that shader is not among the 351 in `globalgamemanagers.assets`. Two `Sprites-Default` materials ship, one in `globalgamemanagers.assets` pointing at an external default-resources shader and one inside `<guid>_unitybuiltinassets_fae0c879bbeef66081109276ec60eb69.bundle` with a stub alongside it. Reuse a live UI material instead of building one from that name.

### 8.8 Deploy-screen and operation imagery

`sharedassets1.assets` holds a coherent family of 12 textures at exactly 1024×512, DXT1, 11 mips: `afghan urban`, `armenia`, `cleanhouse`, `killhouse`, `pvp map 1`, `pvp map big`, `syria compound 01 ISR`, `voa ukraine`, `Compounds 01`, `BOOOAT`, `red_light` and `Screenshot 2026-01-21 211243`. Five of the twelve match a `NavMeshData` filename (`afghan urban`, `armenia`, `syria compound 01 ISR`, `voa ukraine`, `Compounds 01`) and the rest do not, so these being the operation preview thumbnails is inference from the shared format signature rather than a proven binding; a runtime probe would confirm it. Alongside them sit five raw developer screenshots that kept their capture-date filenames (`Screenshot 2024-11-25 235332` 1024×571, `Screenshot 2024-11-26 000527` 1024×574, `Screenshot 2025-06-08 034844` 1024×573 and `Screenshot 2025-12-16 233448` 1024×569, all RGB24 single-mip, plus `Screenshot 2026-06-04 013154` at 1024×1024 DXT1), and `afganv2` at 1024×576.

Only one of that group, `red_light`, is bound by any Material in the file. The rest are referenced directly, which puts the seam on a Texture2D field of a MonoBehaviour or a `RawImage`, not on a Sprite. `level1` contains GameObjects named `Map Image` (×2), `Awesome Map` (×2) and `Maps Header` (×2), which is consistent with that reading but does not prove which field.

HVT dossier portraits are in the same file: `HVT Afghan Urban` and `HVT Afghan Urban 1` at 764×916 DXT1, `Georgia HVT Selfie` at 1024×796, `afghan hvt image` at 512×256, `syria hvt image` at 256×256. The infil markers ship as Sprites `Infil` and `InfilDeselcted`, both 512×512 DXT5, and the second one carries a shipped typo.

The Cerberus op board itself remains unmapped. `CerebusOpboard.MapPrefab` is a `GameObject` reference and `InfilSelectorDisplayer.SpawnMap(GameObject)` instantiates it under `MapParent`, so there is no map Sprite or Texture to find by name. None of the 29,609 GameObject names in `level2` matches any map identity, and none of `sharedassets2`'s 783 textures is op board or infil imagery. Two candidates in that file are decoys. `Minimap_MC_BaseColor`, `Minimap_MC_Normal`, `Minimap_MC_Metalness` and `Minimap_Black_Roughness` (2048² each) are bound by material `MI_Backpack_Minimap 1` on `HDRP/Autodesk Interactive/AutodeskInteractive`, which is a backpack map pouch. `military-world-map-wqhd-1440p-wallpaper` (2048×1024 DXT1) is bound by material `WorldMap` on `HDRP/Lit`, a wall poster whose filename records where it came from. The hub's board dressing is real and editable (`Bulletinboard_BaseColor` 2048², nine 512² marker colour textures, `MilkShaders/WhiteBoard`), but the deploy map is not reachable by file search. The probe that would close this: inside the hub scene, walk `OperationsManager.singleton.ActiveOperations` and log `CerebusOpboard.MapPrefab.name` plus every Renderer, `Image` and `RawImage` material and texture under it.

### 8.9 Adding a custom inventory icon

This is the most common need in this area and it has a clean path, because the icon binding is by name rather than by GUID.

Every mod icon is a Sprite in `sharedassets1.assets`: 2,846 Sprites over 2,829 distinct names, backed by 3,382 Texture2D of which 2,788 are 512×512 and 2,690 of those are DXT5 with a single mip. The sprites are standalone, not atlased, with `m_AtlasTags` empty and `m_SpriteAtlas` null on all 2,846 and a 0.5/0.5 pivot on all 2,846, so a `Sprite.Create` on a fresh texture is a drop-in replacement. Pixels-per-unit is 100 on 2,750 of them, which is `Sprite.Create`'s default; only 8 sprites use 200. The correspondence to items is by name: lowercasing each sprite name and stripping whitespace matches 1,103 of the 1,165 `charactermods_stripped` bundle slugs and 1,417 of the 1,581 `weaponmods_stripped` slugs. 306 sprites match no prefab slug, which is medical items, HUD elements and unshipped content.

Nothing about this is addressable. `CharacterMod` inside a stripped bundle carries only `ModInfoIndex` and `AssetReferenceIndex` as ints; there is no Sprite, no Texture2D and no `CharacterModInfo` in any prefab bundle. The icon is set on the runtime info object instead, and `Il2Cpp.CharacterModInfo` exposes a real setter, `set_UiIcon(UnityEngine.Sprite)`, next to `ModName`, `DisplayName`, `Compatibility`, `ModSlot`, `CharacterSlot`, `isLocked`, `unlockKey` and `unlockReason`. `LoadoutManager` holds the arrays: `get_CharacterModInfo() -> Il2CppReferenceArray<CharacterModInfo>` and `get_WeaponModInfo() -> Il2CppReferenceArray<WeaponModInfo>`, reachable through `LoadoutManager.singleton`.

The runtime route, which survives game patches and is what you should use:

1. Load your PNG into a `Texture2D` at 512×512 and call `Apply()`. Any readable format works; you are not writing back to disk so DXT5 is not required.
2. After the customisation screen has built and `LoadoutManager.singleton` is non-null, take `LoadoutManager.singleton.CharacterModInfo`.
3. Walk that array and match on `info.ModName` against the item's name. Use the exact shipped string, apostrophes doubled where the source has them, for example `'DEVGRU Gold Squadron ''Crusader''` and `''KPYK'' CRC 1U020 FDE Handguard`. Those strings are proven as Sprite names in `sharedassets1.assets`; that `ModName` carries the identical string is inference from the slug correspondence and is worth one debug log to confirm on first run. Do not derive the name from the bundle slug; the slug is the lowercased, whitespace-stripped form and will not compare equal.
4. Assign `info.UiIcon = Sprite.Create(tex, new Rect(0, 0, 512, 512), new Vector2(0.5f, 0.5f))`. Leave pixels-per-unit at the default 100, which is what 2,750 of the 2,846 shipped icon sprites use; passing 200 matches only 8 shipped sprites and changes the icon's layout size in some panels.
5. Re-apply on scene change. The array is rebuilt when `LoadoutManager` re-initialises, and a stale `Sprite` reference will not carry over.

Weapon attachments go through `LoadoutManager.WeaponModInfo` and their icons live in the same file under the same naming convention, but the dumps confirm only that `Il2Cpp.WeaponModInfo` exists as a type; no member list survives, so there is no evidence either way that it exposes a `UiIcon` setter. Treat the weapon side as unverified until you dump that type at runtime; the character side is proven.

The offline route is possible and worse. Open `sharedassets1.assets` in UABEA or AssetsTools.NET, add or replace a Texture2D at 512×512 DXT5 with a single mip named exactly as the mod, then add the paired Sprite pointing at it. It breaks on every game patch, it rewrites a 474 MB object file plus its 2.5 GB of sidecars, and Steam's file verification will restore the original without warning. Use it only if you need the icon correct before any mod code runs.

### 8.10 What is out of reach

Be honest with yourself before you scope a project. File replacement in the Addressables folder cannot touch terrain, vegetation, buildings, props, the skybox, impact decals, blood, bullet holes, explosion graphs, HUD art, menu art, fonts, inventory icons or the op board, because none of those are addressable. UABEA on `.assets` files reaches most of them but not the VFX Graph logic, not the 1,068 baked reflection probes, not the 26 APV baking sets and not the baked lightmaps, and every offline edit is reverted by a depot update or a Steam integrity verify. BRG-drawn vegetation and props are invisible to any runtime code that iterates Renderers, so a mod that looks correct will do nothing to them. And the deploy-screen map imagery has no findable asset name at all, only a prefab reference that needs a live probe to resolve.

The tractable set, in descending order of effort-to-impact: the twelve addressable VFX bundles for muzzle flash, smoke and laser colour; `resources.assets` at 78.9 MB including sidecars for the entire UI icon library; the impact and decal set in `globalgamemanagers.assets`, either offline or by runtime material swap on always-resident objects; `DefaultHDRISky` and `starmap_2020_4k` for a sky pass; the 47 TerrainLayer objects for ground retexture, one shared file at a time; and the runtime `CharacterModInfo.UiIcon` seam for custom icons, which is the only path in this whole area that adds art rather than replacing it.

## 9. Editing a bundle, and the traps

The unit of work is one material bundle. 1,022 of the 1,025 `charactermodmaterials` bundles and 1,202 of the 1,203 `weaponmodmaterials` bundles hold exactly one Material, and 867 of the 1,025 character bundles carry exactly three textures alongside it. Replacing the pixels of one texture in one bundle therefore changes exactly the one Material in that bundle. Nothing outside it can see those texture objects, because Addressables gave every sibling bundle its own private copy. That isolation is what makes file replacement a workable mod format here, and it is also the origin of most of the traps below.

### Locating the bundle for a visible item

You start from what you see in the customisation cabinet and finish at a filename. The chain is item, material asset name, slug, live filename.

The slug is usually the material asset name lowercased, with spaces deleted, underscores kept and `.mat` dropped. Material names come out of `catalog_strings.txt`, so grep that for the product name first. The rule holds exactly for 2,021 of the 2,226 bundles in the two `*materials` families, so treat it as a strong lead rather than a guarantee:

| Material asset | Live bundle |
|---|---|
| `12x5 BLK.mat` | `charactermodmaterials_assets_12x5blk_658479ee2d334e7f96c16dfea7ad1188.bundle` |
| `MI_Patch_JTAC.mat` | `charactermodmaterials_assets_mi_patch_jtac_f0e978d67142c5381a3e67057da29b62.bundle` |
| `MI_Patch_US_BW 1.mat` | `charactermodmaterials_assets_mi_patch_us_bw1_8dc7e17c9336b3839a853c8d7c6ac4d8.bundle` |
| `acog_reticle.mat` | `weaponmodmaterials_assets_acog_reticle_b99dc4f827ae96acc397f9889952f237.bundle` |
| `EXPS3 BLK.mat` | `weaponmodmaterials_assets_exps3blk_e5ddad5a7809e2cfdaf41727dc2f8cea.bundle` |

A further 135 bundles carry a slug that is the normalised material name plus trailing digits, and 70 do not follow the rule at all. Those 70 are where you lose time. `charactermodmaterials_assets_jpc_v2_*` holds nine unrelated materials and none of them is called `jpc_v2`; `.001` suffixes from the source packs get dropped, so `glasses.002` becomes slug `glasses`; and the boonie hats are mislabelled outright, with slug `booniem811` holding the material `Boonie MC` and slug `booniecoyote1` holding `Boonie TIGSTRIPE`.

The derivation is also not reversible. When two materials normalise to the same slug the shipped names gain trailing digits, and the EOTech holographic sight shows the result:

```
weaponmodmaterials_assets_exps3_...       Material 'EXPS3'    MilkShaders/Lit-Template
weaponmodmaterials_assets_exps31_...      Material 'EXPS3 1'  HDRP/Lit
weaponmodmaterials_assets_exps32_...      Material 'Exps3'    MilkShaders/UERemap
weaponmodmaterials_assets_exps321_...     Material 'EXPS3 2'  HDRP/Lit
weaponmodmaterials_assets_exps33_...      Material 'EXPS3 3'  HDRP/Lit
weaponmodmaterials_assets_exps3mc_...     Material 'EXPS3 MC' HDRP/Lit
```

The material names and shaders above are read straight out of `materials.tsv`. The mechanism behind the digits is inference: `Exps3` normalises onto the already-taken `exps3`, `exps31` was already spoken for by `EXPS3 1`, so it lands on `exps32`, which then pushes `EXPS3 2` out to `exps321`. That reading is consistent with every case in the set but is not a documented algorithm, so do not use it to predict a slug. `exps32` does not hold `EXPS3 2`, and 141 of the 2,157 material names in the library appear in more than one bundle. The same optic supplies a clean example: `exps3_reticle1` and `exps3_reticle11` both contain a material named `EXPS3_reticle 1`. Derive the slug, glob for it, then open the file and confirm the Material name before you paint anything.

Resolve the hash by globbing, never by typing it:

```powershell
$aa = 'D:\Games\OPERATOR\OPERATOR_Data\StreamingAssets\aa\StandaloneWindows64'
Get-ChildItem $aa -Filter 'weaponmodmaterials_assets_acog_reticle_*.bundle'
```

### Opening it with UnityPy

The bundles are version stripped. `BundleFile` reports `UnityFS`, version 8, engine version `0.0.0`, so UnityPy has nothing to parse and falls back. Set the fallback before you load anything, and expect a `UnityVersionFallbackWarning` on every load telling you the fallback was used:

```python
import UnityPy
UnityPy.config.FALLBACK_UNITY_VERSION = "6000.3.8f1"

env = UnityPy.load(path)
for o in env.objects:
    d = o.read()
    if o.type.name == "Texture2D":
        print(d.m_Name, d.m_Width, d.m_Height, int(d.m_TextureFormat),
              "mips", d.m_MipCount, d.m_StreamData.path)
```

A stock material bundle dumps like this. The listing is a real read of the stock backup copy of `charactermodmaterials_assets_mi_patch_us_bw_2720380ac7414b8271983117f415cae3.bundle` at 1,506,021 bytes. The file living under that name in the live folder is not this one; it is a 6,839,828-byte modded replacement, for reasons covered in the precedent section below.

```
Shader     ''  m_ParsedForm 'Hidden/Core/FallbackError'
Texture2D  Patch_US_BW_BaseColor   2048x2048  fmt 10  mips 12     <- edit this
Texture2D  patch_us_mask            512x512   fmt 12  mips 10
AssetBundle  d2e33613106a43086854abda8ea2f7c8.bundle              <- internal GUID identity
  m_Container[0] = Assets/1Assets/1CGTrader/3DMA/WSF Operator V2/
                   Textures/PATCHES/PBR_Patch_US_BW_tx/MI_Patch_US_BW.mat
Shader     ''  m_ParsedForm 'Hidden/HDRP/FallbackError'
Material   MI_Patch_US_BW
Shader     ''  m_ParsedForm 'HDRP/Lit'
Texture2D  Patch_US_BW_Normal      1024x1024  fmt 12  mips 11
```

Two structural facts to read off that dump. The AssetBundle's internal name is a GUID, not the filename, so the on-disk filename is only a path string in `catalog.bin` and renaming a repacked file to a different hash keeps the catalog resolving. And in a single-material bundle the one `m_Container` entry is the `.mat`, not a texture, so the addressable key is untouched by a pixel swap and you never register anything. That second fact is a property of single-material bundles, not of the format: `jpc_v2` carries 39 `m_Container` entries.

Read the Shader objects carefully. Their `m_Name` field is empty in every shipped bundle, but the real name sits in `m_ParsedForm.m_Name` and it resolves. 2,399 of the 2,603 materials in the scan report a shader name, and the 204 that do not are all in `*_stripped` prefab bundles whose Shader object lives in another file. Every material bundle also ships one or two `Hidden/*/FallbackError` twins next to the real shader. So `mat.shader.name` is recoverable here, contrary to the usual advice about stripped bundles. It is still not something you need for file replacement. Edit Texture2D image data only, leave the Material, the Shaders and `m_SavedProperties` alone, and the binding keeps working.

### Replacing a texture, matching dimensions and format

Author to the shipped dimensions and the shipped format. Across the 7,247 textures the census is DXT5 4,143, DXT1 2,822, BC7 216, everything else 66. There is no single typical trio; read the original and match it. The three most common shapes per slot, counted over bound `texenvs` rows in each family:

| Slot | `charactermodmaterials` bound | Most common | Second | Third |
|---|---|---|---|---|
| `_BaseColorMap` | 925 | 2048x2048 DXT1 m12 (662) | 1024x1024 DXT1 m11 (140) | 512x512 DXT1 m10 (61) |
| `_NormalMap` | 956 | 2048x2048 DXT5 m12 (397) | 1024x1024 DXT5 m11 (389) | 512x512 DXT5 m10 (67) |
| `_MaskMap` | 905 | 2048x2048 DXT5 m12 (360) | 512x512 DXT5 m10 (257) | 1024x1024 DXT5 m11 (171) |

| Slot | `weaponmodmaterials` bound | Most common | Second | Third |
|---|---|---|---|---|
| `_BaseColorMap` | 1,068 | 2048x2048 DXT1 m12 (734) | 1024x1024 DXT1 m11 (178) | 512x512 DXT1 m10 (40) |
| `_NormalMap` | 1,063 | 2048x2048 DXT5 m12 (591) | 1024x1024 DXT5 m11 (362) | 512x512 DXT5 m10 (50) |
| `_MaskMap` | 1,041 | 2048x2048 DXT5 m12 (387) | 1024x1024 DXT5 m11 (204) | 512x512 DXT1 m10 (132) |

Format codes in the dump are Unity `TextureFormat`: 10 is DXT1/BC1, 12 is DXT5/BC3.

Library-wide, `_BaseColorMap` is the live albedo slot with 2,056 bindings, `_NormalMap` 2,076, `_MaskMap` 2,003. `_MainTex` carries 1,420 and is a mirror rather than a stale slot. Of the 1,294 materials that bind both `_MainTex` and `_BaseColorMap`, all 1,294 point them at the same Texture2D, with zero divergent cases, so the `EXPS3 glass` material is the rule and not an exception. The other 126 `_MainTex` bindings have no `_BaseColorMap` at all, and 115 of those are `HDRP/Autodesk Interactive/AutodeskInteractive`, where `_MainTex` is the only albedo. Consequence for file replacement: one texture edit covers both slots, because both slots point at one object. Consequence for a runtime rebind: write both.

Read the original's format and mip count, then hand both back:

```python
from PIL import Image
img = Image.open("new.png").convert("RGBA")
d.set_image(img, target_format=d.m_TextureFormat, mipmap_count=d.m_MipCount)
d.save()
```

Changing dimensions is allowed and the game accepts it. The shipped GIGN pack below drops a 4096x4096 albedo to 1024x1024 and loads. Changing format is not free: DXT1 to DXT5 doubles that texture's resident memory on every instance, and DXT1 has no alpha channel to promote.

Replacement normals need care beyond format. Sampling `Pouch_Ferro_GP_12x5_OCP_OpenGL_Normal` and `Mag_Magpul_PMAG_GEN3_M3_556_30_Camo_DirectX_Normal` gives R pinned at 255 across every sample, G and B carrying the same value to within DXT block noise, and A varying independently, which is DXT5nm with Y in green and X in alpha. A plain RGB tangent normal injected as DXT5 lights wrong. Re-encode through a Unity project with the Normal Map import type, or hand-pack `(255, Y, Y, X)`. Handedness is a separate question the data does not answer: those two names claim OpenGL and DirectX conventions respectively, which is name-derived inference only, and confirming the green-channel handedness needs a visual check against known geometry.

### Repacking and installing

```python
open("out.bundle", "wb").write(env.file.save(packer="lz4"))
```

Then rename `out.bundle` to the current live filename and copy it over the original. The slug stays, the hash stays, only the bytes change. Back up the original out of the game tree first; Steam's Verify Integrity of Game Files is your undo, and it also restores stock content whenever a depot update lands.

The whole loop. Steps 1 through 5 were run against `weaponmodmaterials_assets_acog_reticle_b99dc4f827ae96acc397f9889952f237.bundle`, a small single-texture target at 59,252 bytes. The output is 84,557 bytes and reloads with its Material `acog_reticle`, its Texture2D `chevon@4x` and its AssetBundle GUID `39280855efe834f6bb0a412ca3333072.bundle` intact.

1. Grep `catalog_strings.txt` for the material name, derive the slug.
2. Glob the live folder for `<slug>_*.bundle`, copy the match to a working directory.
3. Load with `FALLBACK_UNITY_VERSION` set, dump the objects, note `m_TextureFormat` and `m_MipCount`.
4. Export the target Texture2D with `d.image.save(...)`, paint it, reimport with `set_image`.
5. Save with `packer="lz4"`, rename to the current live filename, copy over the original.
6. Reload the output with UnityPy to catch a corrupt write, then check in the loadout cabinet.

Step 6 matters because the cabinet iterates faster than a mission and the failure modes are distinguishable there. A flat untextured surface means `LoadoutManager.PlaceholderMaterial`, which means the load failed. Magenta means a live shader that failed to rebind. `m_LogResourceManagerExceptions` is `true` in the shipped `aa/settings.json`, so a broken bundle produces a visible ResourceManager exception in `Player.log`.

### Trap: the content hash moves when the game patches

Symptom: your mod stops applying after a game update. No error, no crash, no log line, the item simply renders stock again.

The filename grammar is `<family>_assets_<slug>_<32 hex content hash>.bundle`. The hash tracks content, so a patch that rebuilds a group mints new filenames, your file no longer matches any catalog entry, and the game loads its own bundle instead. File mtimes in the live folder show the cadence:

```
2026-02-15    11
2026-04-04  3846   <- base build
2026-04-10     2
2026-04-11     4
2026-07-06  1108   <- content patch, 22% of the library rewritten in one day
2026-07-07     8
2026-07-17    10   <- not Steam; this is a mod install, see below
```

Those seven dates account for all 4,989 bundles. A pack built before 2026-07-06 would have been 22% stale overnight. As of this scan the live folder is set-identical to the 2026-07-16 snapshot in `bundle_names.txt`, all 4,977 hashed names included, so the trap is dormant, not absent. The 12 localization bundles carry no hash in the name and are immune.

The fix is to store the slug and resolve the hash at install time:

```powershell
$aa   = 'D:\Games\OPERATOR\OPERATOR_Data\StreamingAssets\aa\StandaloneWindows64'
$live = @(Get-ChildItem $aa -Filter "charactermodmaterials_assets_${slug}_*.bundle")
if ($live.Count -ne 1) { throw "expected 1 match for $slug, got $($live.Count)" }
Copy-Item $myBundle $live[0].FullName -Force
```

Ship that as an installer, not a manual copy, and the pack survives every patch that does not restructure the groups. Nothing inside your bundle needs to change; the rename alone is the whole repair.

### Trap: one texture name, many bundles

Symptom: you edit a bundle, the change appears on exactly one item, and the five other items you expected to change are untouched. Or the inverse, you search by texture name, edit the first hit, and repaint the wrong item.

Addressables has no shared-asset group in this build. Every bundle carries a private copy of everything it references. 1,179 of the 3,927 distinct texture names appear in two or more bundles, and 4,499 of the 7,247 texture objects are copies of a shared name.

`EXPS3_BaseColor`, 2048x2048 DXT5 with 12 mips in every case, exists in exactly six bundles:

```
weaponmodmaterials_assets_exps3_...              -> Material 'EXPS3'      _BaseColorMap
weaponmodmaterials_assets_exps3blk_...           -> Material 'EXPS3 BLK'  _BaseColorMap
weaponmods_stripped_assets_eotechexps-3blk_...   -> Material 'EXPS3 glass' _BaseColorMap + _MainTex
weaponmods_stripped_assets_eotechexps-3fde_...   -> Material 'EXPS3 glass' _BaseColorMap + _MainTex
weaponmods_stripped_assets_eotechexps-3mcvariant_... -> 'EXPS3 glass'      _BaseColorMap + _MainTex
weaponmods_stripped_assets_eotechg33magniferblk_...  -> 'EXPS3 glass'      _BaseColorMap + _MainTex
```

Editing `exps3blk` repaints the black EXPS-3 housing and leaves the other five copies alone. Note also that four of the six live inside `weaponmods_stripped` prefab bundles, so the rule that art lives in the `*materials` families has exceptions for optics.

The generic names are worse. `MaskMap` is 274 different images across 274 bundles at three resolutions. `LitMask` is 205 images in 205 bundles at five resolutions. `CryePantMask` is 20 copies, all 1024x1024. Two textures sharing a name are not one asset; they are unrelated images that happen to collide. Never search and replace by texture name.

The same duplication cuts the other way and is the reason a camo family is cheap. `Pouch_Ferro_GP_12x5_OCP_OpenGL_Normal` and `MaskMap` are identical across all nine Ferro 12x5 bundles, same dimensions, format, mip count and payload size, and the decoded pixels hash the same in all nine. A six-colourway family therefore costs you six base-colour textures and nothing else:

```
charactermodmaterials_assets_12x5aor2_92b17bde6e6eebc9645be4becdb2e00f   '12x5 AOR2'  2048 DXT1 m12
charactermodmaterials_assets_12x5blk_658479ee2d334e7f96c16dfea7ad1188    '12x5 BLK'   2048 DXT1 m12
charactermodmaterials_assets_12x5gray_79c25e5e12987895033276af175fb1ff   '12x5 GRAY'  2048 DXT1 m12
charactermodmaterials_assets_12x5m81_aabffcb931880edae794e4fffe1980f9    '12x5 M81'   2048 DXT1 m12
charactermodmaterials_assets_12x5mcb_99c7b530b3449d23646e78c9ff73d7bb    '12x5 MCB'   2048 DXT1 m12
charactermodmaterials_assets_12x5od_78bbffea885b82fa10bd009db1e97356     '12x5 OD'    2048 DXT1 m12
shared, identical in all six (and in three mi_pouch_ferro_gp_12x5_* siblings):
  Pouch_Ferro_GP_12x5_OCP_OpenGL_Normal  1024 DXT5 m11  1,398,128 B
  MaskMap                                 512 DXT5 m10    349,552 B
```

In those six the base colour happens to be named exactly like its material, which makes it easy to pick out. Do not generalise that. Across the library only 166 of the 2,056 `_BaseColorMap` bindings have a texture name equal to the material name, and the three `mi_pouch_ferro_gp_12x5_*` siblings in the same family already break it, binding `Pouch_Ferro_GP_12x5_AOR1_BaseColor` to `MI_Pouch_Ferro_GP_12x5_AOR1`. The support maps keep the colourway token of whichever variant was authored first, so `OCP` in that normal's name means nothing.

### Trap: the wrong family

Symptom: you repaint the magazine, it looks right in first person, and the magazines on your chest rig are still stock. Or the reverse.

The same physical object exists once per family and the two families share no naming convention. Of 1,025 `charactermodmaterials` slugs and 1,203 `weaponmodmaterials` slugs, exactly two appear in both, `blk` and `body`. Of the 1,165 and 1,581 stripped-prefab slugs, zero overlap. The Magpul PMAG is the reference case:

```
LOADED IN THE GUN   weaponmodmaterials
  pmag        -> Material 'PMAG'    Pmag30_low_Pmag30_Diffuse  2048 DXT1
  pmag1       -> Material 'PMAG 1'  Pmag30_Base_Color          2048 DXT1
  pmag_40 / pmag_40fde / pmag_d50 / magfde / magfde1 / magmc / mag_glass
  magazine_pmag30_genm3(+1,+2)

WORN ON THE RIG     charactermodmaterials
  mi_pmag_556 -> 'MI_PMAG_556'  PMAG_Black_BaseColor  1024 DXT1
  mi_mag_magpul_pmag_gen3_m3_556_30_blk -> Mag_Magpul_PMAG_GEN3_M3_556_30_BLK_BaseColor 2048 DXT1
  mi_mag_magpul_pmag_gen3_m3_556_30_tan / _glass / m4pmag_sdr / mi_m4pmagfde

BAKED INTO A CARRIER
  jpc_v2 -> Material 'M4Pmag_sdr'  m4Pmag_fbx_low_m4Pmag_sdr_BaseColor  512 DXT1
```

Different material names, different texture names, different resolutions, different shaders, different vendor UVs. There is no shared token to join them on, so you cannot script the pairing; build the table by eye once and record it. Expect four to seven bundles for one visually complete magazine, which is a count off this table rather than a measured figure, and match by appearance rather than by copying files, because the UV layouts come from different source packs.

Two related search hazards. Grepping `charactermods_stripped` for `mag` returns exactly five bundles and all five are sunglasses, because `eyeprogatorzmagnumbluetint` and its four tint siblings contain `magnum`; anchor on `pmag`, `magazine` or `_mag_`. And a name that looks like a part is not always that part: `magBLK` in `weaponmodmaterials` holds `SCAR-H_magazine_20_BLACK_AlbedoTransparency`, and the prefab named `556x45 20rnd PMAG` contains the meshes `SM_AACHB_01a_mag` and `SM_AACHB_01a_mag_follower`, which are not PMAG geometry at all.

### Trap: the pixels are not in the bundle you are editing

Symptom: you patch bytes at the Texture2D object and nothing changes on screen, or your hand-edited bundle produces a black or corrupt texture.

7,191 of the 7,247 textures have `m_StreamData` set, pointing at `archive:/CAB-<id>/CAB-<id>.resS`. The Texture2D object holds a name, a size, a format and an offset; the pixel bytes live in a side stream inside the archive. `Texture2D.image_data` reads back at length 0 for all three textures in a stock `12x5 BLK`, and object-level byte patching writes nothing that renders. You must go through `set_image` and `save` so UnityPy rewrites the payload and the offset and size in `m_StreamData`.

The 56 exceptions are informative. 44 are IES light cookies (`ParallelBeam-2D-IES` and friends, 128x128 RGB9e5Float) that Unity never streams. Two are stock weapon textures with intact mip chains, `Gen5_RearSight_low_Gen5_Rear_Sight_BaseMap` at 1024x1024 RGB24 with 11 mips and `GLOCK17_Slide_Base_color` at 1024x1024 DXT1 with 11 mips. The remaining ten are all `charactermodmaterials` base-colour maps with `m_MipCount` 1, and they are not stock at all. See the precedent section below.

That is the forensic signature of a repacked bundle: the edited texture migrates out of the `.resS` and inlines into the serialized file, so its `m_StreamData.path` goes empty while its untouched siblings keep theirs. Repacking `12x5 BLK` reproduces the signature exactly.

### Trap: the mip chain, which is the expensive one

Symptom: your camo shimmers and crawls at distance and looks noisy rather than broken. It reads as bad art, not as a tooling failure, which is why it ships.

Measured on `12x5 BLK`, 2048x2048 DXT1, original `m_MipCount` 12, re-run for this section against the live bundle:

| Write path | Result mips | Payload bytes | Bundle size |
|---|---|---|---|
| shipped | 12 | 2,796,216 (in `.resS`) | 3,594,078 |
| `d.image = img` | 1 | 2,097,152 | 5,115,950 |
| `d.set_image(img, target_format=..., mipmap_count=12)` | 10 | 2,796,200 | 5,668,737 |

2048 * 2048 / 2 is exactly 2,097,152, one DXT1 level with no chain. The corrected path lands at 10 levels and 2,796,200 bytes, sixteen bytes short of the original because UnityPy stops at the 4x4 DXT block and omits the padded 2x2 and 1x1 levels. That is harmless.

Always pass both arguments. Read and print the original `m_MipCount` first, because 118 stock textures ship with no chain at all and for those the naive setter is fine. `chevon@4x` in the ACOG reticle bundle is 2048x2048 DXT5 with `m_MipCount` 1, which is defensible for a reticle sampled through a scope, and the name is a shipped typo you must match. Only 19 of those 118 are 1024 px or larger, so treat a mips-1 reading on a big albedo as suspicious until you have checked it against a stock copy.

Repacking inflates the file. UnityPy's LZ4 is weaker than the shipped packing and the edited texture leaves the compressed stream. Measured inflation was +43% for the ACOG bundle, 59,252 to 84,557, and +42% to +58% for `12x5 BLK`. Budget roughly 1.5x disk per edited bundle and more when the bundle also carries a large untouched map.

### Does Addressables check anything?

No, and the evidence is layered.

`aa/settings.json` is 864 bytes of Addressables 2.8.1 with `m_CertificateHandlerType` empty in both assembly and class name, `m_IsLocalCatalogInBundle` false, `m_ExtraInitializationData` empty, and exactly one catalog location, key `AddressablesMainContentCatalog`, pointing at a local `catalog.bin` through `ContentCatalogProvider` with `m_Dependencies` empty. There is no signing handler and nothing to sign against. Scanning `catalog.bin`, 3,129,308 bytes, for ASCII runs returns zero strings containing `http`, and only four provider type names appear, `InstanceProvider`, `SceneProvider`, `AssetBundleProvider` and `BundledAssetProvider`, none of them remote. `m_DisableCatalogUpdateOnStart` is false but inert, because `CheckForCatalogUpdates` has no remote catalog to query. `catalog.hash` exists and is only ever compared against a remote catalog that does not exist.

The per-bundle CRC appears to be zero. Anchoring on each bundle's on-disk size as a little-endian uint32 inside `catalog.bin` and reading the preceding uint32 gives 0 for 4,903 of the 4,979 records located, with the 76 nonzero values all near-unique singletons consistent with coincidental matches inside the 241 ambiguous cases. That reads as "Use Asset Bundle CRC was off at build time". This one is offset inference rather than a full binary-catalog parse, so treat it as needs-probe. The settling probe is a MelonLoader Harmony postfix on `AssetBundleRequestOptions.get_Crc`, or logging `options.Crc` in `AssetBundleResource.BeginOperation`, printing bundle name and CRC for the first twenty loads. That is a fifteen-line mod and nobody has run it yet.

The empirical answer does not depend on the inference. Ten edited bundles are installed in this game folder right now, all with contents that differ from the stock backups, and they load. The catalog's stored `BundleSize` is stale for all ten and is not enforced. The 16-byte content hash present in the catalog for 4,967 bundles is `AssetBundleRequestOptions.Hash`, used for remote-bundle caching, and a local `LoadFromFileAsync` does not verify it.

Practical consequence: you may edit bundle bytes freely, you do not need to touch `catalog.bin`, you do not need to recompute anything, and the file size may change by any amount. The only identity that must match is the filename.

### What UnityPy can and cannot do here

UnityPy 1.25.2 reads all 4,989 live bundles with zero errors once `FALLBACK_UNITY_VERSION` is set. It decodes DXT1, DXT5 and BC7 to PIL images, re-encodes them, rewrites the `.resS` stream, preserves the AssetBundle GUID identity, preserves `m_Container`, and repacks with LZ4. That covers the entire texture-replacement surface.

It cannot add an asset to a bundle in a way the game will find, because the addressable key is the `m_Container` entry and the catalog is a fixed binary with absolute offsets into a shared string region. That the offsets are absolute is verified: a record's name-offset field of 83368 pointed exactly at the filename string starting at byte 83368. Inserting a catalog record shifts every downstream offset, so the practical limit of file replacement is N-for-N substitution. A pack that replaces all six 12x5 colourways is fine. A pack that adds a seventh is not possible this way.

Do not build a replacement bundle in a fresh Unity project either. That mints new GUIDs the catalog will not resolve, and two bundles carrying the same internal GUID name cannot both be loaded. Repack the shipped file in place.

Reskins are client-side presentation. That is inference from the architecture rather than a tested result: the art lives in local bundles and the network layer carries no material or texture payload, so a swap should not cross the wire and should carry no desync and no ban surface, and no other player should see it. Confirming it needs a two-client test, which nobody has run.

### The shipped precedent

A French GIGN retexture pack of 10 `charactermodmaterials` bundles is installed in this game folder and is the precedent that this route ships. Its ten files are the entire `2026-07-17` batch in the mtime histogram above, all written at 04:14:10, and they are the ten bundles whose base colour is inlined with `m_MipCount` 1. Stock originals sit in `D:\OperatorHSR\backup\aa_originals_french`, which makes the before-and-after directly measurable:

| Slug | Stock bytes | Installed bytes | Ratio |
|---|---|---|---|
| `mi_patch_us_bw` | 1,506,021 | 6,839,828 | 4.54x |
| `mi_patch_us_bw1` | 3,794,781 | 11,377,796 | 3.00x |
| `mi_patch_us_ir` | 2,382,958 | 7,888,396 | 3.31x |
| `mi_patch_3dma_us_ir` | 1,951,045 | 5,765,552 | 2.96x |
| `mi_patch_jtac` | 746,958 | 2,121,236 | 2.84x |
| `patchgoldensq` | 1,316,362 | 2,790,308 | 2.12x |
| `mi_crye_ls_m81` | 3,649,492 | 10,454,276 | 2.86x |
| `mi_crye_ls_dts` | 3,737,607 | 10,454,292 | 2.80x |
| `mi_crye_pants_dts` | 3,910,536 | 10,454,308 | 2.67x |
| `mi_crye_pants_aor1worn` | 26,292,532 | 35,620,124 | 1.35x |

49,288,292 bytes of stock art becomes 103,766,116 installed, 2.11x, which is the repack cost from the mip-chain section applied ten times.

The pack demonstrates both headline traps. Opening `mi_patch_us_bw` stock and installed side by side gives identical bundle GUID `d2e33613106a43086854abda8ea2f7c8.bundle`, identical `m_Container` path, identical Material name `MI_Patch_US_BW`, identical shader set (`HDRP/Lit` plus `Hidden/Core/FallbackError` and `Hidden/HDRP/FallbackError`), identical `patch_us_mask` and `Patch_US_BW_Normal`, and one difference: `Patch_US_BW_BaseColor` went from 2048x2048 DXT1 with 12 mips streamed to 2048x2048 DXT1 with 1 mip inlined. The pack author hit the mip trap on every one of the ten. `mi_crye_pants_aor1worn` is worse, since the stock albedo is 4096x4096 with 13 mips and the replacement is 1024x1024 with 1, yet the bundle still grew by 9,327,592 bytes.

It also demonstrates the hash trap and its fix. Three bundles in the original download carried hashes that no longer existed in the patched game:

```
mi_crye_ls_m81         c02cb2ac5e2d1b10dcadb347dd2fd85b -> 53bbebe12a2d3e51d027ddf572632290
mi_crye_ls_dts         75c2ac47bbfe4e26a00699cced14cb72 -> d729288d0d695495d2f8b55520d80bd6
mi_crye_pants_dts      890f52d2218ff6be1d92bc30c0e1c414 -> 507de5bdb815452d28f1bccdd683348d
mi_crye_pants_aor1worn 95b045944982978f06b649b80c5ddb14   (already current, unchanged)
the six patch bundles                                     (already current, unchanged)
```

The repair was a rename and nothing else. SHA-256 of the original download matches the installed file on all ten, including the three that were renamed.

One consequence for your own research: a UnityPy sweep of your live folder reports whatever mods you have installed as if it were stock. In this snapshot `MI_CRYE_Pants_DTS` and `MI_CRYE_Pants_AOR1WORN` read as 1024x1024 single-mip albedos against fourteen siblings at 2048x2048 with 12 mips, which looks like a shipping anomaly and is in fact this pack. Scan a verified install, or keep a backup folder and diff against it.

### The runtime alternative, and when it is the better choice

The other route is a MelonLoader mod that mutates materials on live objects instead of replacing files. It survives every patch and every Steam verify, because it does not depend on a filename hash, and you can iterate on it without restarting the game.

It is not available everywhere. `WeaponMod` has the full addressable lifecycle (`SetAssetReferenceIndex`, `TryInitAssetRef`, `DestroyAssetRef`, plus `AmmoReferenceIndex` and `_repackedMagInit`) and no material path whatsoever. No `material`, no `materialIndex`, no `materialBaseColourReference`, no `rendToModify`, no `ActuallySetMaterial`, no `ApplyColorModifier`. A weapon attachment's finish is decided entirely by which prefab and therefore which material bundle was loaded, and there is nothing to intercept. Weapon reskins are a bundle-editing job with no alternative. `CharacterMod` carries the whole colour machinery, so character gear has both options.

Edit the bundle when the target is a weapon attachment, when you are changing texture content rather than a tint, when the change should be permanent and correct in the loadout preview and in your client's rendering of remote players, and when you want it to survive a texture-quality change and a bundle unload cycle. Mutate at runtime when the target is character gear and you want the change conditional per character, per team or toggleable, when you only need a tint or a swap between materials that already exist, or when you want to add a selectable colourway rather than replace one. For a camo project the hybrid is usually right: edit bundles for the art and use `materialIndex` only for the selection layer, since variants of one item already share a `ModName` and differ only by that int.

Two runtime costs to plan for. `LoadoutManager` reloads texture detail on a quality change, exposing `CurrentTextureQuality`, `TextureQualityChanged()` and `OnTextureQualityIncreased`, so a runtime swap must re-apply on that event or the player raising texture quality reverts it. And the addressable unload is reference counted and timer deferred through `CheckForUnUsedAddressable`, so hold residency with `SetAddressableAssetUnloadTime` while you work or the coroutine pulls the asset out from under you.

### The shared-material hazard

`CharacterMod.material` points at the single Material asset inside the addressable bundle, not a per-instance copy. `CharacterMod` exposes only whole-Material handles, `material`, `tattooMaterial` and `rendToModify`, and no `MaterialPropertyBlock` member of any kind, so the game itself assigns Materials wholesale and offers no per-instance escape hatch.

Calling `charMod.material.SetTexture("_BaseColorMap", myTex)` mutates the loaded asset. Every character in the session wearing that item changes on your client, including your rendering of remote players and the loadout preview mannequin. The mutation outlives the CharacterMod you reached through, persists as long as the bundle's refcount stays above zero, survives scene changes if anything holds that reference, and is gone when the bundle finally unloads and reloads. A runtime reskin is therefore re-application, not one-shot application.

The safe pattern is your own code, not the game's. Write to `Renderer.material`, which makes Unity instantiate a private copy scoped to that renderer, or set a `MaterialPropertyBlock` through `Renderer.SetPropertyBlock`. Never `Renderer.sharedMaterial`, and never the `CharacterMod.material` handle, unless you actually intend to change every character at once.

One caveat on the two shaders you will meet. `HDRP/Lit` covers 1,314 materials and `MilkShaders/Lit-Template` covers 439, and both expose the same HDRP Lit property block by name, including `_BaseColorMap`, `_MaskMap` and `_NormalMap`, so a property block keyed by name reaches either. They are still different shader assets with different variant and keyword sets, so reassigning one material's shader to the other is not a recolour path; it compiles and renders wrong. Property blocks under HDRP also break SRP batching for that renderer.

Pick the route before you start authoring. Bundle replacement is zero code, N-for-N, and needs a re-scan after every patch. Runtime mutation is additive and patch-proof and costs you a mod, a re-application hook, and the discipline to keep every write scoped to one renderer.

## Data files

Everything in `data/textures/`, all tab separated with a header row unless noted.

| file | what it holds |
|---|---|
| `textures.tsv` | 7,247 textures: bundle, name, width, height, format, mip count, streamed flag, size |
| `materials.tsv` | 2,603 materials: bundle, name, shader |
| `texture-bindings.tsv` | 8,252 rows of material, shader property, and the texture bound to it |
| `material-properties.tsv` | the 35 properties that carry authored variation, per material |
| `bundle-objects.tsv` | per-bundle object census, useful for spotting outliers |
| `texture-tree.txt` | the generated tree, category then item then colourway then bound maps |
| `texture-tree.json` | the same tree, machine readable |
| `item-products.json` | 620 long-tail asset names resolved to the products they model |
| `research-findings.json` | the 125 raw findings, each with a confidence marker |
| `summary.json` | the format, resolution, property and shader censuses |

Regenerate any of it with `tools/BundleScan/scan.py` against your own install. The content hashes in
bundle filenames change when the game patches, so a scan taken against one build will not line up with
another. Rescan after an update rather than trusting a stale inventory.
