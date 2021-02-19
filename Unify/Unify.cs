using BepInEx;
using BepInEx.IL2CPP;
using HarmonyLib;
using Reactor;
using Unify.Patches;

namespace Unify
{
    [BepInPlugin(Id, Name, Version)]
    [BepInProcess("Among Us.exe")]
    [BepInDependency(ReactorPlugin.Id)]
    public class UnifyPlugin : BasePlugin
    {
        public const string Id = "reactor.unify";
        private const string Name = "Unify";
        private const string Version = "0.1.0";

        public Harmony Harmony { get; } = new Harmony(Id);

        public override void Load()
        {
            RegionsPatch.Patch();
            
            Harmony.PatchAll();
        }
    }
}