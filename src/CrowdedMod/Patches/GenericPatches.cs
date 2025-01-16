using System.Linq;
using AmongUs.GameOptions;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace CrowdedMod.Patches;

internal static class GenericPatches {
    
    // I did not find a use of this method, but still patching for future updates
    // maxExpectedPlayers is unknown, looks like server code tbh
    [HarmonyPatch(typeof(GameOptionsData), nameof(GameOptionsData.AreInvalid))]
    public static class InvalidOptionsPatches
    {
        public static bool Prefix(GameOptionsData __instance, [HarmonyArgument(0)] int maxExpectedPlayers)
        {
            return __instance.MaxPlayers > maxExpectedPlayers ||
                   __instance.NumImpostors < 1 ||
                   __instance.NumImpostors + 1 > maxExpectedPlayers / 2 ||
                   __instance.KillDistance is < 0 or > 2 ||
                   __instance.PlayerSpeedMod is <= 0f or > 3f;
        }
    }

    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Update))]
    public static class GameStartManagerUpdatePatch
    {
        private static string? fixDummyCounterColor;
        public static void Prefix(GameStartManager __instance)
        {
            if (GameData.Instance == null || __instance.LastPlayerCount == GameData.Instance.PlayerCount)
            {
                return;
            }

            if (__instance.LastPlayerCount > __instance.MinPlayers)
            {
                fixDummyCounterColor = "<color=#00FF00FF>";
            }
            else if (__instance.LastPlayerCount == __instance.MinPlayers)
            {
                fixDummyCounterColor = "<color=#FFFF00FF>";
            }
            else
            {
                fixDummyCounterColor = "<color=#FF0000FF>";
            }
        }

        public static void Postfix(GameStartManager __instance)
        {
            if (fixDummyCounterColor == null)
            {
                return;
            }
            
            __instance.PlayerCounter.text = $"{fixDummyCounterColor}{GameData.Instance.PlayerCount}/{GameManager.Instance.LogicOptions.MaxPlayers}";
            fixDummyCounterColor = null;
        }
    }

    [HarmonyPatch(typeof(GameSettingMenu), nameof(GameSettingMenu.OnEnable))]
    public static class GameSettingMenu_OnEnable // Credits to https://github.com/Galster-dev/GameSettingsUnlocker
    {
        public static void Prefix(ref GameSettingMenu __instance)
        {
            __instance.HideForOnline = new Il2CppReferenceArray<Transform>(0);
        }
    }

    // Will be patched with signatures later when BepInEx reveals it
    // [HarmonyPatch(typeof(InnerNetServer), nameof(InnerNetServer.HandleNewGameJoin))]
    // public static class InnerNetSerer_HandleNewGameJoin
    // {
    //     public static bool Prefix(InnerNetServer __instance, [HarmonyArgument(0)] InnerNetServer.Player client)
    //     {
    //         if (__instance.Clients.Count >= 15)
    //         {
    //             __instance.Clients.Add(client);
    //
    //             client.LimboState = LimboStates.PreSpawn;
    //             if (__instance.HostId == -1)
    //             {
    //                 __instance.HostId = __instance.Clients.ToArray()[0].Id;
    //
    //                 if (__instance.HostId == client.Id)
    //                 {
    //                     client.LimboState = LimboStates.NotLimbo;
    //                 }
    //             }
    //
    //             var writer = MessageWriter.Get(SendOption.Reliable);
    //             try
    //             {
    //                 __instance.WriteJoinedMessage(client, writer, true);
    //                 client.Connection.Send(writer);
    //                 __instance.BroadcastJoinMessage(client, writer);
    //             }
    //             catch (Il2CppException exception)
    //             {
    //                 Debug.LogError("[CM] InnerNetServer::HandleNewGameJoin MessageWriter 2 Exception: " +
    //                                exception.Message);
    //                 // ama too stupid for this 
    //                 // Debug.LogException(exception.InnerException, __instance);
    //             }
    //             finally
    //             {
    //                 writer.Recycle();
    //             }
    //
    //             return false;
    //         }
    //
    //         return true;
    //     }
    // }

    [HarmonyPatch(typeof(GameOptionsMenu), nameof(GameOptionsMenu.Initialize))]
    public static class GameOptionsMenu_Initialize
    {
        public static void Postfix(GameOptionsMenu __instance)
        {
            var numberOptions = __instance.GetComponentsInChildren<NumberOption>();

            var impostorsOption = numberOptions.FirstOrDefault(o => o.Title == StringNames.GameNumImpostors);
            if (impostorsOption != null)
            {
                impostorsOption.ValidRange = new FloatRange(1, CrowdedModPlugin.MaxImpostors);
            }

        }
    }
}
