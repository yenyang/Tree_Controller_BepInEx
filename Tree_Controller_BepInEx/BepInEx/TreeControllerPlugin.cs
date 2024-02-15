// <copyright file="TreeControllerPlugin.cs" company="Yenyangs Mods. MIT License">
// Copyright (c) Yenyangs Mods. MIT License. All rights reserved.
// </copyright>

#if BEPINEX

namespace Tree_Controller
{
    using BepInEx;
    using Game;
    using Game.Common;
    using HarmonyLib;

    /// <summary>
    /// Mod entry point for BepInEx configuaration.
    /// </summary>
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, "Tree Controller", "1.1.2")]
    [HarmonyPatch]
    public class TreeControllerPlugin : BaseUnityPlugin
    {
        /// <summary>
        /// A static instance of the IMod for mod entry point.
        /// </summary>
        internal static TreeControllerMod _mod;

        /// <summary>
        /// Patches and Injects mod into game via Harmony.
        /// </summary>
        public void Awake()
        {
            _mod = new ();
            _mod.OnLoad();
            _mod.Logger.Info($"{nameof(TreeControllerPlugin)}.{nameof(InjectSystems)}");
            Harmony.CreateAndPatchAll(typeof(TreeControllerPlugin).Assembly, MyPluginInfo.PLUGIN_GUID);
        }

        [HarmonyPatch(typeof(SystemOrder), nameof(SystemOrder.Initialize), new[] { typeof(UpdateSystem) })]
        [HarmonyPostfix]
        private static void InjectSystems(UpdateSystem updateSystem)
        {
            _mod.OnCreateWorld(updateSystem);
        }
    }
}
#endif