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
        /// Patches ObjectToolSystem.TrySetPrefab.
        /// If not using tree controller tool or selecting multiple prefabs while brushing, original methods acts as normal.
        /// Will skip it and return false if Tree Controller tool is active tool and an appropriate prefab is selected.
        /// Will select or unselect prefabs if selecting multiple prefabs while brushing.
        /// </summary>
        /// <param name="prefab">The prefab that is trying to be set.</param>
        /// <param name="__result">The result for the original method.</param>
        /// <returns>True if not skipping method. False if skipping method.</returns>
        public static bool Prefix(ref PrefabBase prefab, ref bool __result)
        {
            ILog log = TreeControllerMod.Instance.Logger;
            log.Debug($"{nameof(ObjectToolSystemTrySetPrefabPatch)}.{nameof(Prefix)}");
            TreeControllerTool treeControllerTool = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<TreeControllerTool>();
            ToolSystem toolSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<ToolSystem>();
            ObjectToolSystem objectToolSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<ObjectToolSystem>();
            if (toolSystem.activeTool != treeControllerTool && toolSystem.activeTool != objectToolSystem && toolSystem.activeTool.toolID != "Line Tool")
            {
                log.Debug($"{nameof(ObjectToolSystemTrySetPrefabPatch)}.{nameof(Prefix)} returned because active tool.");
                return true;
            }

            if (toolSystem.selected != Entity.Null || (toolSystem.activeTool == objectToolSystem && objectToolSystem.mode != ObjectToolSystem.Mode.Brush && objectToolSystem.mode != ObjectToolSystem.Mode.Create))
            {
                log.Debug($"{nameof(ObjectToolSystemTrySetPrefabPatch)}.{nameof(Prefix)} returned because something is selected or object tool mode is neither brush nor create.");
                return true;
            }

            log.Debug($"{nameof(ObjectToolSystemTrySetPrefabPatch)}.{nameof(Prefix)} toolSystem.selected = {toolSystem.selected.Index}.{toolSystem.selected.Version}.");
            log.Debug($"{nameof(ObjectToolSystemTrySetPrefabPatch)}.{nameof(Prefix)} objectToolSystem.mode = {objectToolSystem.mode}");
            TreeControllerUISystem treeControllerUISystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<TreeControllerUISystem>();
            PrefabSystem prefabSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<PrefabSystem>();
            if (!prefabSystem.TryGetEntity(prefab, out Entity prefabEntity))
            {
                log.Debug($"{nameof(ObjectToolSystemTrySetPrefabPatch)}.{nameof(Prefix)} returned because could not find prefab entity.");
                return true;
            }

            if (prefabSystem.EntityManager.HasComponent<Vegetation>(prefabEntity))
            {
                log.Debug($"{nameof(ObjectToolSystemTrySetPrefabPatch)}.{nameof(Prefix)} has vegetation component");
                bool ctrlKeyPressed = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;
                if ((toolSystem.activeTool == objectToolSystem && objectToolSystem.brushing == false)
                || (toolSystem.activeTool == objectToolSystem && !ctrlKeyPressed && !treeControllerUISystem.RecentlySelectedPrefabSet)
                || toolSystem.activeTool.toolID == "Line Tool")
                {
                    log.Debug($"{nameof(ObjectToolSystemTrySetPrefabPatch)}.{nameof(Prefix)} resetting selecting and returning.");
                    treeControllerTool.ClearSelectedTreePrefabs();
                    treeControllerUISystem.ResetPrefabSets();
                    treeControllerTool.SelectTreePrefab(prefab);
                    return true;
                }

                if (toolSystem.activeTool == objectToolSystem)
                {
                    List<PrefabBase> selectedPrefabs = treeControllerTool.GetSelectedPrefabs();
                    if (selectedPrefabs.Contains(prefab) && selectedPrefabs.Count > 1 && !treeControllerUISystem.UpdateSelectionSet && !treeControllerUISystem.RecentlySelectedPrefabSet)
                    {
                        treeControllerTool.UnselectTreePrefab(prefab);
                        selectedPrefabs.Remove(prefab);
                        if (selectedPrefabs.Contains(toolSystem.activePrefab))
                        {
                            log.Debug($"{nameof(ObjectToolSystemTrySetPrefabPatch)}.{nameof(Prefix)} Set prefab {prefab.name} to active prefab {toolSystem.activePrefab.name}.");
                            prefab = toolSystem.activePrefab;
                            log.Debug($"{nameof(ObjectToolSystemTrySetPrefabPatch)}.{nameof(Prefix)} selected prefabs contains active prefab returning.");
                            return true;
                        }
                        else
                        {
                            log.Debug($"{nameof(ObjectToolSystemTrySetPrefabPatch)}.{nameof(Prefix)} Set prefab {prefab.name} to active prefab {selectedPrefabs[0].name}.");
                            if (selectedPrefabs[0] is ObjectPrefab)
                            {
                                objectToolSystem.prefab = selectedPrefabs[0] as ObjectPrefab;
                            }

                            log.Debug($"{nameof(ObjectToolSystemTrySetPrefabPatch)}.{nameof(Prefix)} selected prefabs does not contain active prefab returning false.");
                            __result = true;
                            return false;
                        }
                    }
                    else if (!selectedPrefabs.Contains(prefab) && !treeControllerUISystem.UpdateSelectionSet)
                    {
                        treeControllerTool.SelectTreePrefab(prefab);
                        log.Debug($"{nameof(ObjectToolSystemTrySetPrefabPatch)}.{nameof(Prefix)} Selecting {prefab.name}.");
                        return true;
                    }
                    else if (selectedPrefabs.Contains(toolSystem.activePrefab) || treeControllerUISystem.RecentlySelectedPrefabSet)
                    {
                        log.Debug($"{nameof(ObjectToolSystemTrySetPrefabPatch)}.{nameof(Prefix)} recently selected prefab set.");
                        return true;
                    }
                    else
                    {
                        log.Debug($"{nameof(ObjectToolSystemTrySetPrefabPatch)}.{nameof(Prefix)} Set prefab {prefab.name} to active prefab {selectedPrefabs[0].name}.");
                        prefab = selectedPrefabs[0];
                        __result = true;
                        return false;
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
