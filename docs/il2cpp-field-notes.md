# IL2CPP field notes

Lessons from modding a Unity 6 IL2CPP title with MelonLoader and HarmonyX. Most of these take hours to find and none of them are obvious from the API surface.

## Compiling is not evidence

A successful build proves an interop member exists. It proves nothing about its signature semantics, its staticness at runtime, whether it is populated when you read it, or whether calling it does what the name suggests.

## Native crashes are not catchable

An access violation inside il2cpp tears the process down. `try`/`catch` around the call does not save you, because no managed exception is ever raised. Null checking a managed wrapper tests the pointer, not the liveness of the native object behind it, so a wrapper that passes `!= null` can still be a destroyed object. Be especially careful dereferencing objects during teardown paths.

## Never scan the object table per frame

`Resources.FindObjectsOfTypeAll` is one of the most expensive calls in Unity. Calling it every frame will cost most of the frame rate. The subtle version of this bug is an early return that skips the done flag, so a scene where the target never appears scans forever. Make sure the not found path still advances state.

## Field accessors cannot be patched

Harmony refuses to patch il2cpp field accessors, reporting that the method is a field accessor. If a value is exposed as a raw field rather than a property, you cannot intercept reads. Write to it on a timer, or find the consumer and patch that instead.

## Mirror RPCs receive on UserCode_

Networked games weave RPC methods. Patching the sender stub does nothing on receiving clients. Hook `UserCode_<MethodName>` for the receive side. In single player as host, target RPCs to clients may never fire at all, so never build a feature on the assumption that a `TargetRpc` runs locally.

## Il2CppStructArray marshalling

Managed arrays passed across the boundary can be collected mid call. Fill an `Il2CppStructArray<T>` element by element and hand that over. Large allocations are riskier than small ones, which is why mesh building works best in per object chunks rather than one giant buffer.

## AssetBundles are awkward, not impossible

Corrected by Remero, visuals developer at ODG, who loaded a custom bundle in this game while working on the M107. An earlier version of this document said bundles were refused outright. That was wrong, and his result is consistent with what our own loader turned out to be doing.

What is true is that the ordinary managed entry points do not work. `LoadFromFile` throws a missing method because the icall is span bound and those bindings are not generated. `LoadFromMemory` loses its byte array to the collector mid call, whether the array is managed or `Il2CppSystem`. The async wrapper returns a request whose bundle is null, because the marshalled path string arrives empty on the other side.

The path that does work is to bypass the wrappers. Find the static field `NativeMethodInfoPtr_LoadFromFileAsync_Internal_*` on `AssetBundle` by reflection, call it through `IL2CPP.il2cpp_runtime_invoke` with the path converted by `IL2CPP.ManagedStringToIl2Cpp`, and wrap the returned pointer in an `AssetBundleCreateRequest`. Reading `.assetBundle` on that request forces synchronous completion. This needs `AllowUnsafeBlocks` for the argument array. The sync internal is not usable because it has no MethodInfo pointer to invoke, only the span delegate.

Expect the bundle to load and the materials not to survive. Shaders are stripped and centralised in a player build, so material references in a bundle resolve to nothing and assets arrive untextured. Rebind after load with `mat.shader = Shader.Find(mat.shader.name)`, which is the same trick the game itself uses when it relinks stripped materials. Build the bundle in the exact Unity version the game ships, and be aware that the game's own bundles have their version header stripped to `0.0.0`.

Runtime construction, which is what `src/RuntimeAssets` does, remains a reasonable choice for geometry and audio because it sidesteps all of this and needs no editor. It is a preference, not the only option.

## When a filter returns nothing, stop theorising

The single most effective debugging technique here is a log only inventory dump bound to a hotkey. Enumerate every object of the type in question and print every relevant property side by side, including the ones your filter depends on, then read the data and look for a correlation. Two separate multi hour bugs in this game were solved in one pass each by doing this after several plausible theories had already failed. Two details matter. Dump everything regardless of the filter under suspicion, since gating the diagnostic on the suspect condition defeats the point. And a one shot dump that fires when a UI first appears can capture a half populated state, so prefer an explicit key.

While reading those dumps, note that grepping for `active` also matches `inactive`. Match with the leading space.

## Booleans on UI objects may be state, not identity

A flag that looks like a property of an object can turn out to describe the mode the object is currently being displayed in. In this game a marker flag meaning "this is an extraction point" reads true on ordinary insertion points as well, because the same locations serve both roles, and it changes during a session. Filtering on it discarded every valid result. If a boolean gives inconsistent answers across sessions, stop trusting it and find a structural discriminator instead.

## Collections often do not contain what the name implies

Before iterating a list looking for something, dump the list and confirm the thing is in it. A parent collection on a character system here contains only child attachment sockets, never the top level items, which are exposed as individual properties. Several rewrites failed because they searched a collection that structurally could not hold the target.

## Deployment while the game runs

The DLL is locked while the game is open. A background job that waits for the process to exit and then copies works well, but if you cancel or replace that job you will ship a stale build and waste an entire test cycle. Always verify the deployed file timestamp matches the build output before launching.

## Console output during startup

Writing to the console from a melon during IL2CPP startup was enough to prevent the game from loading. If you want a banner, log it through the loader rather than taking over the console.
