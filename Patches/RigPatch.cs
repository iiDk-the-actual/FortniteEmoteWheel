using HarmonyLib;

namespace FortniteEmoteWheel.Patches
{
    [HarmonyPatch(typeof(VRRig), "OnDisable")]
    public class RigPatch
    {
        public static bool Prefix(VRRig __instance)
        {
            return !(__instance == VRRig.LocalRig);
        }
    }

    [HarmonyPatch(typeof(VRRig), "PostTick")]
    public class RigPatch2
    {
        public static bool Prefix(VRRig __instance) =>
            !__instance.isLocal || __instance.enabled;
    }
}
