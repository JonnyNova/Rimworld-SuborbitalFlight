using System.Reflection;
using Verse;
using Harmony;

namespace OHUShips
{
    public class Mod : Verse.Mod
    {
        public Mod(ModContentPack content) : base(content)
        {
            var harmony = HarmonyInstance.Create("FrontierDevelopments.SuborbitalFlight");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}