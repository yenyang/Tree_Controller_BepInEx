// <copyright file="ObjectToolSystemTrySetPrefabPatch.cs" company="Yenyangs Mods. MIT License">
// Copyright (c) Yenyangs Mods. MIT License. All rights reserved.
// </copyright>

namespace Tree_Controller.Patches
{
    using System.Collections.Generic;
    using Colossal.Logging;
    using Game.Prefabs;
    using Game.Tools;
    using HarmonyLib;
    using Tree_Controller.Tools;
    using Unity.Entities;
    using UnityEngine.InputSystem;

    /// <summary>
    /// Patches ObjectToolSystem.TrySetPrefab. If not using tree controller tool, original methods acts as normal. Will skip it and return false if Tree Controller tool is active tool and an appropriate prefab is selected.
    /// </summary>
    [HarmonyPatch(typeof(ObjectToolSystem), "TrySetPrefab")]
    public class ObjectToolSystemTrySetPrefabPatch
    {
        /// <summary>
        /// Patches ObjectToolSystem.TrySetPrefab. If not using tree controller tool, original methods acts as normal. Will skip it and return false if Tree Controller tool is active tool and an appropriate prefab is selected.
        /// <param name="prefab">The parameter from method call.</param>
        /// <param name="__result">The result for the original method.</param>
        /// <returns>True if not skipping method. False if skipping method.</returns>
        public static bool Prefix(ref PrefabBase prefab, ref bool __result)
        {
            ILog log = TreeControllerMod.Instance.Logger;
            log.Debug($"{nameof(ObjectToolSystemTrySetPrefabPatch)}.{nameof(Prefix)}");
            TreeControllerTool treeControllerTool = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<TreeControllerTool>();
            ToolSystem toolSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<ToolSystem>();
            ObjectToolSystem objectToolSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<ObjectToolSystem>();
            if ((toolSystem.activeTool != treeControllerTool && toolSystem.activeTool != objectToolSystem)
                || (toolSystem.activeTool == objectToolSystem && objectToolSystem.brushing == false)
                || (toolSystem.activeTool == objectToolSystem && !Keyboard.current[Key.LeftCtrl].isPressed))
            {
                return true;
            }

            PrefabSystem prefabSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<PrefabSystem>();
            Entity prefabEntity = prefabSystem.GetEntity(prefab);
            if (prefabSystem.EntityManager.HasComponent<TreeData>(prefabEntity) && !prefabSystem.EntityManager.HasComponent<PlaceholderObjectElement>(prefabEntity))
            {
                if (toolSystem.activeTool == objectToolSystem)
                {
                    TreeControllerUISystem treeControllerUISystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<TreeControllerUISystem>();
                    List<PrefabBase> selectedPrefabs = treeControllerTool.GetSelectedPrefabs();
                    if (selectedPrefabs.Contains(prefab) && selectedPrefabs.Count > 1)
                    {
                        treeControllerTool.UnselectTreePrefab(prefab);
                        selectedPrefabs.Remove(prefab);
                        treeControllerUISystem.UpdateSelectionSet = true;
                        if (selectedPrefabs.Contains(toolSystem.activePrefab))
                        {
                            log.Debug($"{nameof(ObjectToolSystemTrySetPrefabPatch)}.{nameof(Prefix)} Set prefab {prefab.name} to active prefab {toolSystem.activePrefab.name}.");
                            prefab = toolSystem.activePrefab;
                        }
                        else
                        {
                            log.Debug($"{nameof(ObjectToolSystemTrySetPrefabPatch)}.{nameof(Prefix)} Set prefab {prefab.name} to active prefab {selectedPrefabs[0].name}.");
                            prefab = selectedPrefabs[0];
                        }

                        return true;
                    }
                    else if (!selectedPrefabs.Contains(prefab))
                    {
                        treeControllerTool.SelectTreePrefab(prefab);
                        log.Debug($"{nameof(ObjectToolSystemTrySetPrefabPatch)}.{nameof(Prefix)} Selecting {prefab.name}.");
                        return true;
                    }
                    else
                    {
                        return true;
                    }
                }

                log.Debug($"{nameof(ObjectToolSystemTrySetPrefabPatch)}.{nameof(Prefix)} Bypassing ObjectTool");
                __result = false;
                return false;
            }

            return true;
        }
    }
}
