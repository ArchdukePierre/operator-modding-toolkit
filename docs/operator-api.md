# OPERATOR API reference

Verified against a live build. Types live in `Assembly-CSharp` under the `Il2Cpp` namespace unless stated otherwise. Networking is Mirror. Animation is KINEMATION for the first person viewmodel and RootMotion FinalIK for the networked third person body.

Regenerate any of this yourself with `tools/InteropInspector`.

## Weapons and loadout

`LoadoutManager` is a singleton via the static `singleton` property. `Game_Weapons` is an `Il2CppSystem.List<GameObject>` and appending to it registers a weapon, after which the game's own resolver `GetWeaponID(string)` finds it by GameObject name. `setWeapon(string)`, `SpawnWeapon(string)` and `getPrimaryWeapon()` complete the surface.

Base weapons carry `WeaponV3`, not `WeaponV2`. `WeaponV3` exposes `displayName`, `weaponSlotType`, `IsProduction`, `gunStats`, `gunFunction`, `weaponRecoil`, `weaponAnimation` and Mirror SyncVars. `GunStats` and `GunFunction` are plain data objects reached through those properties rather than components in their own right.

`WeaponMods` handles attachments. `WeaponFamily` and `magazineCompatibility` are strings and magazine gating is name based rather than caliber based. `BASE_JSON_MOD_STRING` holds the default build as JSON listing part names, socket paths and offsets, and `RebuildWeaponWithModString` reassembles from it. A base weapon prefab is close to an empty skeleton, with the visible weapon assembled at runtime from that JSON.

Giving a weapon to a player goes through `PlayerNetworking.SpawnWeapon(string name, string modJSON, PlayerNetworking.WeaponSlot slot)`. Do not call `WeaponV3.Equip` directly, it throws inside without player context. Clone templates under a disabled holder so their components never wake, since clones of an already awoken Mirror object die during spawn.

Wall pickups are separate. The armoury uses `N_StaticWeaponSpawner`, whose `WeaponReference` GameObject property can be repointed at another weapon to take over a rack slot.

## Character customisation

`CharacterCustomisationController` drives the cabinet UI. `StartCustomisation(PlayerNetworking)` and `button_StopCustomisation()` bracket the session, with `Start_Selection(string)`, `OnSelectedMod(string)`, `OnSelected(string)`, `Start_Selection_Modding(CharacterModParent)` and `CancelSelection()` handling picking.

`CharacterCustomisation` holds the worn kit. The important structural fact is that `modParents` contains only child and attachment sockets such as armour plates, helmet covers, night vision mounts and patch slots. The worn garment for each category is not in that list. It is a direct property: `HeadGear`, `EyeWear`, `EarPro`, `Face`, `Voice`, `Shirt`, `Gloves`, `Wrist`, `Pants`, `Shoes`, `Vest`, `Belt`, `Backpack` and `Tattoo`.

Equipping is `SpawnMod(CharacterMod prefab, CharacterModParent parent)`. A top level garment passes `null` as the parent and the call routes by the item's own `modSlot`. A child mod passes its own socket. The game's own garment swap performs no removal first, while child swaps call `RemoveMod(ModdingSlot)` before spawning.

An item is a `CharacterMod`, identified by `ModInfo.ModName`. Colour and camo variants share one item name and differ by `materialIndex`, applied with `ActuallySetMaterial()`. Resolve a name back to a prefab with `LoadoutManager.singleton.GetCharacterMod(string)`.

Note that child socket names repeat across items, so a name alone is not a unique key. Qualify it with the owning item.

The cabinet stages edits as a local preview. The networked `CharacterModSync.characterModsJSON` is only rewritten on exit, which is why other players see the old kit until the session ends, and why reading that JSON from inside the cabinet returns stale data.

## Missions and operations

A mission is a four level chain. `CERBERUS_OPERATION` is a board entry and holds `CERBERUS_TARGETPACKAGE` children. Each target package points at a `CerebusOpboard`, which is the actual mission definition. `TARGETPACKAGE_DETAILS` carries `OPERATION_SCENE`, `DISPLAY_NAME` and `INFILTRATION_TIME`.

`CerebusOpboard` is where the useful configuration lives: `MapPrefab`, `AvailableInfils` as a string array, `MinAI` and `MaxAI` with `SetAI_COUNT(float)`, `Difficulty`, `HVTSpawnIsRandom`, `PrepTimeInSeconds`, `TargetRaidTime`, `SituationReport`, `requireEnoughInfils` and `requireEnoughExfils`, and `Start_Operation()` to launch.

`OperationsManager` performs the launch through `StartOperation(string sceneName, int prepTime, string displayName, string infilTime, string activeAchievement, float targetTime)`, which commands to the server and ends in `SERVER_LoadOperation()`. `DebugStartOperation(string sceneName, string infilName, GameMode)` is a convenient direct entry point for testing.

The playable level is a real Unity scene named by `OPERATION_SCENE`. Since bundles are rejected, new scenes cannot be added, so a custom mission has to reuse an existing scene as a container.

## Infiltration

`InfiltrationManager` owns the flow. `UserCode_RPC_StartInfil(int index)` reports the chosen infil at mission start and is the reliable arming signal, including for the host.

`MapInfilMarker` represents a point on the board. `MarkerIndex` matches the index reported by the RPC. `InfilName` is load bearing, because the game resolves the spawn through it, and renaming it drops the player into an empty void. `TextUI.text` is display only and safe to change.

`IsGroundInfil` and `IsHeliInfil` are reliable. `IsExfil` is not. Walk in points double as extraction points, so that flag reads true on most ground markers and its value changes during a session. Filtering on it discards valid results.

Only the currently displayed mission's markers are `activeInHierarchy`, typically mirrored across several lobby monitor copies. Pooled template markers report `MarkerIndex` of minus one and should be ignored.

Local placement happens in `GameManager.MovePlayerToSpawn(Vector3, Quaternion)`. Prefixing it and rewriting the position is the clean way to spawn somewhere else, but it does not reliably fire for a solo host, so a fallback that waits for a real non origin position is needed. Position replication is SmoothSync, not NetworkTransform, and large teleports need `teleportOwnedObjectFromOwner()`.

Vehicle insertions seat the player through `HelicopterV3.SitInAssignedSeat` and hand off at `AutoExitHelicopter`. Ride progress is readable from `InfilVehicleAnimation.animator` as normalized time, which is comparable across maps despite differing clip lengths.

## Doors

`DoorV2` is a Mirror networked physics door. `DoorHandleV2` is the interaction half and there is one per side, paired through `RivalDoorHandle` and distinguished by `IsFrontHandle`. It exposes `TryStartInteraction()`, `StartHandleInteraction()`, `StartCenterInteraction()`, `StopInteraction()` and `CalculateAndApplyDoorForces(float)`, with distance tuning fields for pull, push and damping. `Handle` and `Center` are FinalIK `InteractionObject` instances driven by an `InteractionSystem`, which is what makes characters reach for the handle properly. `DoorHitBox` and `DoorIK` complete the set.

Clone an existing door rather than authoring one. The paired handles and IK objects are tedious to reconstruct.

## AI navigation

The game uses A* Pathfinding Project, not Unity NavMesh, in `Il2CppAstarPathfindingProject`. The main class is `Il2Cpp.AstarPath` in the global namespace while everything else sits under `Il2CppPathfinding`, so search accordingly.

Runtime rebuild is available. `Scan(NavGraph)` and `ScanAsync(NavGraph)` do a full rebuild, `UpdateGraphs(Bounds)` does a cheap incremental one over a region, and `AddWorkItem(Action)` mutates the graph safely. `RecastGraph` bakes from scene colliders, so geometry added at runtime needs real colliders before it will be walkable. `RecastNavmeshModifier` gives per object control over area, solidity and scan inclusion. `BotOffmeshLinkHandler` indicates off mesh links exist for ladders and vaults.

Unity's own `NavMeshBuilder.BuildNavMeshData` and the `Unity.AI.Navigation` package are also present as a fallback, but the bots ride A*.

The AI stack itself is `BrainAI`, `AIPatrol`, `AiTeam`, `EyesAI`, `BotSpawner`, `BotSpawnDetails` and `BotNetworking`, with `PlayerSpawnDetails` for players and `RaidManager` orchestrating.

## Movement

The player controller is Character Movement Fundamentals. `MasterController` exposes `rigidBody`, `walkerController`, `mover`, `velocityLastFrame` and `totalWeight`. `AdvancedWalkerController` provides `SetMomentum`, `GetMomentum`, `IsGrounded`, `OnGroundContactLost` and settable `gravity`, `airControlRate` and `airFriction`.

Two things matter for any airborne work. The controller re-grounds a teleported player on the next physics step, so hold the new position for a short window after moving. And `SetMomentum` does not cap fall speed, because gravity keeps accumulating. Clamp `rigidBody.linearVelocity.y` directly in a `HandleMomentum` postfix instead. This is Unity 6, so it is `linearVelocity` and not `velocity`.

Fall damage funnels through `MasterController.ProcessFall(Vector3)` with `TakeFallDamageStanding` and `TakeFallDamageProne` variants. A returning false prefix suppresses it. Patches on movement classes fire for every character on the client, so always gate on a cached local instance pointer.
