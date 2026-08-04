# DoorV2 dead-field audit

Scope: `DoorV2` plus the other first-party scripts on `_DoorV2_BASE.prefab` (`DoorHandleV2` x2, `DoorHitBox`,
`ShootableDoorPart` x3, `MilkRigidbodySync` and its base `MilkTransformSync`). The remaining components on the
prefab are third-party and were not audited: `NodeLink2`, `NavmeshCut`, `NetworkIdentity`,
`InteractionObject`/`Target`, `PolyFewHost`.

Written for milk_drinker01's cleanup pass. The code is his; this is only a reference pass over it.

Method note: unlike the rest of this repository, which is derived from disassembly, this pass was done at script level, so it can see commented-out code and exact line numbers. Line numbers are accurate to the revision reviewed and will drift.

---

## DoorV2.cs — dead fields

| Field | Line | Status |
|---|---|---|
| `DoorModelParent` | 22 | Only referenced inside the commented-out `TryAutoFindCollider` (107-121). |
| `DoorMask` | 24 | Only reference is the commented-out raycast at `DoorHandleV2.cs:294`. The `= 4545` default is also nonsense as a LayerMask. |
| `navCutOpenSize` | 55 | Only in the commented-out NavMeshCut resize block in `HandleAIBlockers` (455-481). |
| `navCutCloseSize` | 56 | Same. |

**Not dead, but not obvious from this file.** `latchCollider`, `HingeTopCollider`, `HingeBottomCollider` (63-65)
are never touched by `DoorV2` itself. They are read by `SlapChargeExplosive.cs:237-239` to pick the nearest
breach point. Keep them.

Everything else in `DoorV2` is live, including `NavMeshCut` (disabled in `DoorDie`), `canBlowup`, and the whole
dead-door block, which `DoorHandleV2` consumes.

## DoorHandleV2.cs — dead fields

| Field | Line | Status |
|---|---|---|
| `RivalDoorHandle` | 11 | Zero references anywhere. Leftover from the old `DoorHandle.cs`, where it is also commented out. |
| `allowedDistanceToPlayerDamper` | 21 | Zero references. |
| `GrabbedHandle` | 13 | Write-only. Set at 333/370/410, never read. `GrabbedCenter` *is* read, so only this one is dead. |
| `raycastTransform` | 38 | Write-only. `Start()` allocates a `new GameObject()` and parents it, then nothing ever uses it. |

`raycastTransform` is the only one with a runtime cost. Every handle spawns a permanent empty GameObject named
"GameObject" at load, two per door, for nothing. That whole `Start()` can go.

## Clean

`DoorHitBox`, `ShootableDoorPart`, `MilkRigidbodySync`, `MilkTransformSync` — no unused fields.

---

## Before deleting

`DoorModelParent`, `DoorMask`, `navCutOpenSize`, `navCutCloseSize` and `RivalDoorHandle` are public serialized
fields with values already baked into roughly 40 door prefabs. Removing the field drops that serialized data.
That does not matter while they stay unused, but it is not recoverable if the field is wanted again later.
