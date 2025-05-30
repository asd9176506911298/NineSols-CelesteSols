using HarmonyLib;

namespace CelesteSols;

[HarmonyPatch]
public class Patches {
    [HarmonyPatch(typeof(Player), "Fall")]
    [HarmonyPrefix]
    private static bool HookFall(Player __instance) {
        if (CelesteSols.isFallEnable)
            return true;
        else
            return false;

        return true;
    }
}