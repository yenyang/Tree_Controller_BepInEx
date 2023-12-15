// <copyright file="ObjectToolSystemTrySetPrefabPatch.cs" company="Yenyangs Mods. MIT License">
// Copyright (c) Yenyangs Mods. MIT License. All rights reserved.
// </copyright>

namespace Tree_Controller.Patches
{
    using Game.Prefabs;
    using Game.Tools;
    using HarmonyLib;
    using Tree_Controller.Tools;
    using Unity.Entities;

    [HarmonyPatch(typeof(ObjectToolSystem), "TrySetPrefab")]
    public class ObjectToolSystemTrySetPrefabPatch
    {
        /// <summary>
        /// Patches ObjectToolSystem.TrySetPrefab. If not using tree controller tool, original methods acts as normal. Will skip it and return false if Tree Controller tool is active tool and an appropriate prefab is selected.
        /// <param name="prefab">The parameter from method call.</param>
        /// <param name="__result">The result for the original method.</param>
        /// <returns>True if not skipping method. False if skipping method.</returns>
        public static bool Prefix(PrefabBase prefab, bool __result)
        {
            TreeControllerMod.Instance.Logger.Debug($"{nameof(ObjectToolSystemTrySetPrefabPatch)}.{nameof(Prefix)}");
            TreeControllerTool treeControllerTool = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<TreeControllerTool>();
            ToolSystem toolSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<ToolSystem>();
            if (toolSystem.activeTool != treeControllerTool)
            {
                return true;
            }

            PrefabSystem prefabSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<PrefabSystem>();
            Entity prefabEntity = prefabSystem.GetEntity(prefab);
            if (prefabSystem.EntityManager.HasComponent<TreeData>(prefabEntity) && !prefabSystem.EntityManager.HasComponent<PlaceholderObjectElement>(prefabEntity))
            {
                __result = false;
                TreeControllerMod.Instance.Logger.Debug($"{nameof(ObjectToolSystemTrySetPrefabPatch)}.{nameof(Prefix)} Bypassing ObjectTool");
                return false;
            }

            return true;
        }
    }
}
