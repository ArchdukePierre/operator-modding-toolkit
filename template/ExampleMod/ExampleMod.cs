using System;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(ExampleMod.Main), "Example Mod", "1.0.0", "you")]
[assembly: MelonGame(null, null)]

namespace ExampleMod
{
    public class Main : MelonMod
    {
        MelonPreferences_Entry<bool> _enabled;

        public override void OnInitializeMelon()
        {
            var cfg = MelonPreferences.CreateCategory("ExampleMod");
            _enabled = cfg.CreateEntry("Enabled", true);

            LoggerInstance.Msg("loaded. F6 prints the local player.");

            // Hooking pattern. AccessTools returns null rather than throwing when the name is wrong,
            // so log the miss instead of assuming it worked.
            Hook(typeof(Il2Cpp.GameManager), "MovePlayerToSpawn", nameof(Post_MovePlayerToSpawn));
        }

        void Hook(Type type, string method, string handler)
        {
            try
            {
                var mi = AccessTools.Method(type, method);
                if (mi == null) { LoggerInstance.Warning($"hook miss: {type.Name}.{method}"); return; }
                HarmonyInstance.Patch(mi, postfix: new HarmonyMethod(typeof(Main), handler));
                LoggerInstance.Msg($"hooked {type.Name}.{method}");
            }
            catch (Exception e) { LoggerInstance.Error($"hook failed {type.Name}.{method}: {e.Message}"); }
        }

        // Postfixes fire for every instance on the client, so gate on the local one when it matters.
        static void Post_MovePlayerToSpawn(ref Vector3 position)
        {
            MelonLogger.Msg($"[ExampleMod] player placed at {position}");
        }

        public override void OnUpdate()
        {
            if (_enabled == null || !_enabled.Value) return;
            if (!Input.GetKeyDown(KeyCode.F6)) return;

            try
            {
                var pm = Il2Cpp.PlayerMaster.MyPlayerMaster;
                LoggerInstance.Msg(pm != null ? $"local player alive: {pm.currentlySpawnedAndAlive}" : "no local player");
            }
            catch (Exception e) { LoggerInstance.Error(e.Message); }
        }
    }
}
