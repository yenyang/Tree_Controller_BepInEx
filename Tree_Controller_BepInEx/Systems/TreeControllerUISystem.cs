﻿// <copyright file="TreeObjectDefinitionUISystem.cs" company="Yenyangs Mods. MIT License">
// Copyright (c) Yenyangs Mods. MIT License. All rights reserved.
// </copyright>

#define VERBOSE
namespace Tree_Controller.Tools
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using cohtml.Net;
    using Colossal.Logging;
    using Game.Objects;
    using Game.Prefabs;
    using Game.SceneFlow;
    using Game.Tools;
    using Game.UI;
    using Game.Vehicles;
    using Tree_Controller.Systems;
    using Tree_Controller.Utils;
    using Unity.Entities;
    using static Tree_Controller.Tools.TreeControllerTool;

    /// <summary>
    /// UI system for Object Tool while using tree prefabs.
    /// </summary>
    public partial class TreeControllerUISystem : UISystemBase
    {
        private readonly List<PrefabID> m_VanillaDeciduousPrefabIDs = new()
        {
            { new PrefabID("StaticObjectPrefab", "EU_AlderTree01") },
            { new PrefabID("StaticObjectPrefab", "BirchTree01") },
            { new PrefabID("StaticObjectPrefab", "NA_LondonPlaneTree01") },
            { new PrefabID("StaticObjectPrefab", "NA_LindenTree01") },
            { new PrefabID("StaticObjectPrefab", "NA_HickoryTree01") },
            { new PrefabID("StaticObjectPrefab", "EU_ChestnutTree01") },
            { new PrefabID("StaticObjectPrefab", "OakTree01") },
        };

        private readonly List<PrefabID> m_VanillaEvergreenPrefabIDs = new()
        {
            { new PrefabID("StaticObjectPrefab", "PineTree01") },
            { new PrefabID("StaticObjectPrefab", "SpruceTree01") },
        };

        private readonly List<PrefabID> m_VanillaWildBushPrefabs = new()
        {
            { new PrefabID("StaticObjectPrefab", "GreenBushWild01") },
            { new PrefabID("StaticObjectPrefab", "GreenBushWild02") },
            { new PrefabID("StaticObjectPrefab", "FlowerBushWild01") },
            { new PrefabID("StaticObjectPrefab", "FlowerBushWild02") },
        };

        private readonly Dictionary<string, TCSelectionMode> ToolModeLookup = new()
        {
            { "YYTC-single-tree", TCSelectionMode.SingleTree },
            { "YYTC-building-or-net", TCSelectionMode.WholeBuildingOrNet },
            { "YYTC-radius", TCSelectionMode.Radius },
            { "YYTC-whole-map", TCSelectionMode.WholeMap },
        };

        private readonly Dictionary<TCSelectionMode, string> ToolModeButtonLookup = new ()
        {
            { TCSelectionMode.SingleTree, "YYTC-single-tree" },
            { TCSelectionMode.WholeBuildingOrNet, "YYTC-building-or-net" },
            { TCSelectionMode.Radius, "YYTC-radius" },
            { TCSelectionMode.WholeMap, "YYTC-whole-map" },
        };

        private View m_UiView;
        private ToolSystem m_ToolSystem;
        private PrefabSystem m_PrefabSystem;
        private ObjectToolSystem m_ObjectToolSystem;
        private TreeObjectDefinitionSystem m_TreeObjectDefinitionSystem;
        private TreeControllerTool m_TreeControllerTool;
        private string m_InjectedJS = string.Empty;
        private ILog m_Log;
        private PrefabBase m_LastObjectToolPrefab;
        private List<BoundEventHandle> m_BoundEvents;
        private Dictionary<string, List<PrefabID>> m_PrefabSetsLookup;
        private string m_TreeControllerPanelScript = string.Empty;
        private string m_SelectionRowItemScript = string.Empty;
        private string m_ToolModeItemScript = string.Empty;
        private string m_AgeChangingToolModeItemScript = string.Empty;
        private bool m_ToolIsActive;
        private bool m_ObjectToolPlacingTree = false;
        private string m_SelectedPrefabSet = string.Empty;
        private bool m_PreviousPanelsCleared = false;

        public void ResetPrefabSets()
        {
            m_SelectedPrefabSet = string.Empty;
            UIFileUtils.ExecuteScript(m_UiView, "yyTreeController.selectedPrefabSet = \"\";");
            foreach (KeyValuePair<string, List<PrefabID>> keyValuePair in m_PrefabSetsLookup)
            {
                // This script removes selected from any previously selected prefab sets if they are found.
                UIFileUtils.ExecuteScript(m_UiView, $"yyTreeController.element = document.getElementById(\"{keyValuePair.Key}\"); if (yyTreeController.element != null) {{  yyTreeController.element.classList.remove(\"selected\"); }}");
            }
        }

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            m_Log = TreeControllerMod.Instance.Logger;
            m_ToolSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<ToolSystem>();
            m_PrefabSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<PrefabSystem>();
            m_UiView = GameManager.instance.userInterface.view.View;
            m_ObjectToolSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<ObjectToolSystem>();
            m_TreeObjectDefinitionSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<TreeObjectDefinitionSystem>();
            m_TreeControllerTool = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<TreeControllerTool>();
            ToolSystem toolSystem = m_ToolSystem; // I don't know why vanilla game did this.
            m_ToolSystem.EventToolChanged = (Action<ToolBaseSystem>)Delegate.Combine(toolSystem.EventToolChanged, new Action<ToolBaseSystem>(OnToolChanged));
            ToolSystem toolSystem2 = m_ToolSystem;
            toolSystem2.EventPrefabChanged = (Action<PrefabBase>)Delegate.Combine(toolSystem2.EventPrefabChanged, new Action<PrefabBase>(OnPrefabChanged));
            m_BoundEvents = new ();
            m_PrefabSetsLookup = new Dictionary<string, List<PrefabID>>()
            {
                { "YYTC-wild-deciduous-trees", m_VanillaDeciduousPrefabIDs },
                { "YYTC-evergreen-trees", m_VanillaEvergreenPrefabIDs },
                { "YYTC-wild-bushes", m_VanillaWildBushPrefabs },
            };

            if (m_UiView != null)
            {
                m_InjectedJS = UIFileUtils.ReadJS(Path.Combine(UIFileUtils.AssemblyPath, "ui.js"));
                m_TreeControllerPanelScript = UIFileUtils.ReadHTML(Path.Combine(UIFileUtils.AssemblyPath, "YYTC-selection-mode-item.html"), "if (document.getElementById(\"tree-controller-panel\") == null) { yyTreeController.div.className = \"tool-options-panel_Se6\"; yyTreeController.div.id = \"tree-controller-panel\"; yyTreeController.ToolColumns = document.getElementsByClassName(\"tool-side-column_l9i\"); if (yyTreeController.ToolColumns[0] != null) yyTreeController.ToolColumns[0].appendChild(yyTreeController.div); yyTreeController.setupYYTCSelectionModeItem(); }");
                m_SelectionRowItemScript = UIFileUtils.ReadHTML(Path.Combine(UIFileUtils.AssemblyPath, "YYTC-selection-mode-content.html"), "if (document.getElementById(\"YYTC-selection-mode-item\") == null) { yyTreeController.div.className = \"item_bZY\"; yyTreeController.div.id = \"YYTC-selection-mode-item\"; yyTreeController.AgesElement = document.getElementById(\"YYTC-tree-age-item\"); if (yyTreeController.AgesElement != null) yyTreeController.AgesElement.insertAdjacentElement('afterend', yyTreeController.div); yyTreeController.setupYYTCSelectionModeItem(); }");
                m_ToolModeItemScript = UIFileUtils.ReadHTML(Path.Combine(UIFileUtils.AssemblyPath, "YYTC-tool-mode-content.html"), "if (document.getElementById(\"YYTC-tool-mode-item\") == null) { yyTreeController.div.className = \"item_bZY\"; yyTreeController.div.id =\"YYTC-tool-mode-item\"; yyTreeController.OptionsPanels = document.getElementsByClassName(\"tool-options-panel_Se6\"); if (yyTreeController.OptionsPanels[0] != null) { yyTreeController.OptionsPanels[0].appendChild(yyTreeController.div); yyTreeController.setupToolModeButtons(); } }");
                m_AgeChangingToolModeItemScript = UIFileUtils.ReadHTML(Path.Combine(UIFileUtils.AssemblyPath, "YYTC-tool-mode-content.html"), "if (document.getElementById(\"YYTC-tool-mode-item\") == null) { yyTreeController.div.className = \"item_bZY\"; yyTreeController.div.id =\"YYTC-tool-mode-item\"; yyTreeController.panel = document.getElementById(\"tree-controller-panel\"); if (yyTreeController.panel != null) { yyTreeController.panel.appendChild(yyTreeController.div); yyTreeController.setupToolModeButtons(); } }");
            }
            else
            {
                m_Log.Info($"{nameof(TreeControllerUISystem)}.{nameof(OnCreate)} m_UiView == null");
            }

            m_Log.Info($"{nameof(TreeControllerUISystem)}.{nameof(OnCreate)}");

            Enabled = false;
            base.OnCreate();
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            if (m_ToolSystem.activeTool.toolID != null)
            {
                if (m_ToolSystem.activeTool.toolID == "Line Tool")
                {
                    Random random = new ();
                    if (random.Next(10) >= 9)
                    {
                        m_Log.Debug($"{nameof(TreeControllerUISystem)}.{nameof(OnUpdate)} Line tool active");
                    }

                    return;
                }
            }

            if (m_ToolSystem.activeTool == m_TreeControllerTool && m_UiView != null)
            {
                // This script creates the Tree Controller object if it doesn't exist.
                UIFileUtils.ExecuteScript(m_UiView, "if (yyTreeController == null) var yyTreeController = {};");

                if (m_ToolIsActive == true)
                {
                    if (m_TreeControllerTool.GetPrefab() == null)
                    {
                        UIFileUtils.ExecuteScript(m_UiView, $"if (document.getElementById(\"tree-controller-panel\") == null) engine.trigger('YYTC-tree-controller-panel-missing');");
                    }
                    else
                    {
                        UIFileUtils.ExecuteScript(m_UiView, $"if (document.getElementById(\"YYTC-selection-mode-item\") == null) engine.trigger('YYTC-selection-mode-item-missing');");
                    }

                    return;
                }

                if (m_PreviousPanelsCleared == false && m_TreeControllerTool.GetPrefab() == null)
                {
                    UnshowObjectToolPanelItems(); // It is very important to have this here, because putting it in OnToolChanged or ActiveAgeChange causes crashes.
                    UIFileUtils.ExecuteScript(m_UiView, $"{DestroyElementByID("YYTC-tool-mode-item")} {DestroyElementByID("YYTC-selection-mode-item")} {DestroyElementByID("YYTC-radius-row")}");
                    m_PreviousPanelsCleared = true;
                    return; // Wait a frame for panel changes to occur;
                }
                else if (m_PreviousPanelsCleared == false)
                {
                    UIFileUtils.ExecuteScript(m_UiView, $"{DestroyElementByID("YYTC-rotation-row")} {DestroyElementByID("YYTC-ActivateAgeChange")} {DestroyElementByID("YYTC-ActivatePrefabChange")}");

                    // This unregisters the events.
                    foreach (BoundEventHandle boundEvent in m_BoundEvents)
                    {
                        m_UiView.UnregisterFromEvent(boundEvent);
                    }

                    m_BoundEvents.Clear();

                    UnshowTreeControllerToolPanel();
                    m_PreviousPanelsCleared = true;
                    return;  // Wait a frame for panel changes to occur;
                }

                bool[] ages = m_TreeControllerTool.GetSelectedAges();

                // This script sets the ages map variable in JS.
                UIFileUtils.ExecuteScript(m_UiView, $"yyTreeController.selectedAges = new Map([[\"YYTC-child\", {BoolToString(ages[0])}],[\"YYTC-teen\", {BoolToString(ages[1])}],[\"YYTC-adult\", {BoolToString(ages[2])}],[\"YYTC-elderly\", {BoolToString(ages[3])}],[\"YYTC-dead\", {BoolToString(ages[4])}],]);");

                // This script sets the selectionMode variable in JS.
                UIFileUtils.ExecuteScript(m_UiView, $"yyTreeController.selectionMode = \"{ToolModeButtonLookup[m_TreeControllerTool.SelectionMode]}\";");

                // This script sets the radius variable in JS.
                UIFileUtils.ExecuteScript(m_UiView, $"yyTreeController.radius = {m_TreeControllerTool.Radius};");

                // This script sets the selectedPrefabSet variable in JS.
                UIFileUtils.ExecuteScript(m_UiView, $"yyTreeController.selectedPrefabSet = \"{m_SelectedPrefabSet}\";");

                // This script injects all the JS functions if they do not exist.
                UIFileUtils.ExecuteScript(m_UiView, m_InjectedJS);

                if (m_TreeControllerTool.GetPrefab() == null) // Tree age changing only.
                {
                    // This script builds the Tool mode item row, tree age item row. And checks if radius row needs to be added.
                    UIFileUtils.ExecuteScript(m_UiView, $"{m_TreeControllerPanelScript} yyTreeController.selectionModeItem = document.getElementById(\"YYTC-selection-mode-item\"); if (yyTreeController.selectionModeItem != null)  yyTreeController.buildTreeAgeItem(yyTreeController.selectionModeItem,'afterend'); yyTreeController.checkIfNeedToBuildRadius();");

                    // This script builds and sets up the Tool Mode item row.
                    UIFileUtils.ExecuteScript(m_UiView, $"{m_AgeChangingToolModeItemScript} yyTreeController.selectedToolMode = document.getElementById(\"YYTC-ActivateAgeChange\"); if (yyTreeController.selectedToolMode != null) yyTreeController.selectedToolMode.classList.add(\"selected\"); ");

                    // Register event callbacks from UI JavaScript.
                    m_BoundEvents.Add(m_UiView.RegisterForEvent("YYTC-ActivatePrefabChange", (Action)ActivatePrefabChange));
                    m_BoundEvents.Add(m_UiView.RegisterForEvent("YYTC-tree-controller-panel-missing", (Action)ResetPanel));
                }
                else // Prefab Changing
                {
                    m_ObjectToolPlacingTree = false;

                    // This script builds Tree Age item.
                    UIFileUtils.ExecuteScript(m_UiView, "yyTreeController.entities = document.getElementsByClassName(\"tool-options-panel_Se6\"); if (yyTreeController.entities[0] != null) { if (yyTreeController.entities[0].firstChild != null) { yyTreeController.buildTreeAgeItem(yyTreeController.entities[0].firstChild, 'beforebegin'); } }");

                    // This script builds the Tool mode item row, tree age item row. And checks if radius row needs to be added.
                    UIFileUtils.ExecuteScript(m_UiView, $"{m_SelectionRowItemScript} yyTreeController.checkIfNeedToBuildRadius();");

                    // This scripts builds the prefab sets row.
                    UIFileUtils.ExecuteScript(m_UiView, $"yyTreeController.ageItem = document.getElementById(\"YYTC-tree-age-item\"); if (yyTreeController.ageItem != null) yyTreeController.buildPrefabSetsRow(yyTreeController.ageItem,'afterend');");

                    // Register event callbacks from UI JavaScript.
                    m_BoundEvents.Add(m_UiView.RegisterForEvent("YYTC-Prefab-Set-Changed", (Action<string>)ChangePrefabSet));
                    m_BoundEvents.Add(m_UiView.RegisterForEvent("YYTC-ActivateAgeChange", (Action)ActivateTreeControllerTool));
                    m_BoundEvents.Add(m_UiView.RegisterForEvent("YYTC-selection-mode-item-missing", (Action)ResetPanel));

                    // This script builds and sets up the Tool Mode item row.
                    UIFileUtils.ExecuteScript(m_UiView, $"{m_ToolModeItemScript} yyTreeController.selectedToolMode = document.getElementById(\"YYTC-ActivatePrefabChange\"); if (yyTreeController.selectedToolMode != null) yyTreeController.selectedToolMode.classList.add(\"selected\");");
                }


                // Register event callbacks from UI JavaScript.
                m_BoundEvents.Add(m_UiView.RegisterForEvent("YYTC-ChangeSelectedAges", (Action<bool[]>)ChangeSelectedAges));
                m_BoundEvents.Add(m_UiView.RegisterForEvent("YYTC-ToolModeChanged", (Action<string>)ChangeToolMode));
                m_BoundEvents.Add(m_UiView.RegisterForEvent("TClog", (Action<string>)LogFromJS));
                m_BoundEvents.Add(m_UiView.RegisterForEvent("YYTC-AdjustRadius", (Action<int>)ChangeRadius));
                m_BoundEvents.Add(m_UiView.RegisterForEvent("YYTC-brush-trees", (Action)ActivateBrushTrees));
                m_BoundEvents.Add(m_UiView.RegisterForEvent("YYTC-plop-tree", (Action)ActivatePlopTrees));

                m_ToolIsActive = true;
                m_PreviousPanelsCleared = false;
                return;
            }

            if (m_ToolIsActive)
            {
                UnshowTreeControllerToolPanel();
                UnshowObjectToolPanelItems();
                UIFileUtils.ExecuteScript(m_UiView, $"{DestroyElementByID("YYTC-tool-mode-item")} {DestroyElementByID("YYTC-selection-mode-item")} {DestroyElementByID("YYTC-radius-row")}");
                m_ToolIsActive = false;
                return;  // Wait a frame for panel changes to occur;
            }

            if (m_ToolSystem.activeTool != m_ObjectToolSystem || m_UiView == null)
            {
                Enabled = false;
                return;
            }

            if (m_ObjectToolPlacingTree)
            {
                UIFileUtils.ExecuteScript(m_UiView, $"if (document.getElementById(\"YYTC-tree-age-item\") == null) engine.trigger('YYTC-tree-age-item-missing');");
            }

            // This script creates the Tree Controller object if it doesn't exist.
            UIFileUtils.ExecuteScript(m_UiView, "if (yyTreeController == null) var yyTreeController = {};");

            // This script looks for any img srcs that use brush and then adds new Change tool buttons to the grandparent element.
            UIFileUtils.ExecuteScript(m_UiView, $"if (document.getElementById(\"YYTC-ActivateAgeChange\") == null) {{ yyTreeController.tagElements = document.getElementsByTagName(\"img\");  for (yyTreeController.i = 0; yyTreeController.i < yyTreeController.tagElements.length; yyTreeController.i++) {{  if (yyTreeController.tagElements[yyTreeController.i].src == \"Media/Tools/Object%20Tool/Brush.svg\") {{  yyTreeController.buildActivateToolButton(\"YYTC-ActivateAgeChange\", \"coui://uil/Standard/ReplaceTreeAge.svg\", yyTreeController.tagElements[yyTreeController.i].parentNode.parentNode);    yyTreeController.buildActivateToolButton(\"YYTC-ActivatePrefabChange\", \"coui://uil/Standard/Replace.svg\", yyTreeController.tagElements[yyTreeController.i].parentNode.parentNode);  }} }} }}");

            if (m_ObjectToolPlacingTree == false)
            {
                bool[] ages = m_TreeControllerTool.GetSelectedAges();

                // This script sets the ages map variable in JS.
                UIFileUtils.ExecuteScript(m_UiView, $"yyTreeController.selectedAges = new Map([[\"YYTC-child\", {BoolToString(ages[0])}],[\"YYTC-teen\", {BoolToString(ages[1])}],[\"YYTC-adult\", {BoolToString(ages[2])}],[\"YYTC-elderly\", {BoolToString(ages[3])}],[\"YYTC-dead\", {BoolToString(ages[4])}],]);");

                // This script sets the selected prefab set variable in JS.
                UIFileUtils.ExecuteScript(m_UiView, $"yyTreeController.selectedPrefabSet = \"{m_SelectedPrefabSet}\";");

                // This scrip sets the random rotation variable in JS.
                UIFileUtils.ExecuteScript(m_UiView, $"yyTreeController.randomRotation = {BoolToString(m_TreeObjectDefinitionSystem.RandomRotation)};");

                // This script defines all the JS functions if they do not exist.
                UIFileUtils.ExecuteScript(m_UiView, m_InjectedJS);

                // This script builds Tree Age item used for brushing trees.
                UIFileUtils.ExecuteScript(m_UiView, "yyTreeController.entities = document.getElementsByClassName(\"tool-options-panel_Se6\"); if (yyTreeController.entities[0] != null) { if (yyTreeController.entities[0].firstChild != null) { yyTreeController.buildTreeAgeItem(yyTreeController.entities[0].firstChild, 'beforebegin'); } }");

                // This unregisters the events.
                foreach (BoundEventHandle boundEvent in m_BoundEvents)
                {
                    m_UiView.UnregisterFromEvent(boundEvent);
                }

                m_BoundEvents.Clear();

                m_BoundEvents.Add(m_UiView.RegisterForEvent("YYTC-Random-Rotation-Changed", (Action<bool>)ChangeRandomRotation));
                m_BoundEvents.Add(m_UiView.RegisterForEvent("YYTC-ActivateAgeChange", (Action)ActivateTreeControllerTool));
                m_BoundEvents.Add(m_UiView.RegisterForEvent("YYTC-ActivatePrefabChange", (Action)ActivatePrefabChange));
                m_BoundEvents.Add(m_UiView.RegisterForEvent("TClog", (Action<string>)LogFromJS));
                m_BoundEvents.Add(m_UiView.RegisterForEvent("YYTC-Prefab-Set-Changed", (Action<string>)ChangePrefabSet));
                m_BoundEvents.Add(m_UiView.RegisterForEvent("YYTC-ChangeSelectedAges", (Action<bool[]>)ChangeSelectedAges));
                m_BoundEvents.Add(m_UiView.RegisterForEvent("YYTC-tree-age-item-missing", (Action)ResetPanel));

                m_ObjectToolPlacingTree = true;
            }

            if (m_ObjectToolSystem.brushing)
            {
                // This script destroys rotation and plop age row while not plopping single tree.
                UIFileUtils.ExecuteScript(m_UiView, "yyTreeController.destroyElementByID(\"YYTC-rotation-row\");");

                // This script buils the prefab Sets row for brushing sets of trees.
                // Need a new anchor.
                UIFileUtils.ExecuteScript(m_UiView, "yyTreeController.ageRow = document.getElementById(\"YYTC-tree-age-item\"); if (yyTreeController.ageRow != null) { yyTreeController.buildPrefabSetsRow(yyTreeController.ageRow, 'afterend'); }");
            }
            else
            {
                // This script destroys the tree age item row and prefab sets rows that were used for brushing trees.
                UIFileUtils.ExecuteScript(m_UiView, "yyTreeController.destroyElementByID(\"YYTC-prefab-sets-row\");");

                // This script builds the rotation row icon.
                // Need a new anchor.
                UIFileUtils.ExecuteScript(m_UiView, "yyTreeController.ageRow = document.getElementById(\"YYTC-tree-age-item\"); if (yyTreeController.ageRow != null) { yyTreeController.buildRotationRow(yyTreeController.ageRow, 'afterend'); }");
            }

            base.OnUpdate();
        }

        /// <summary>
        /// Removes items and unregisters events for Object tool panel.
        /// </summary>
        private void UnshowObjectToolPanelItems()
        {
            m_Log.Debug($"{nameof(TreeControllerUISystem)}.{nameof(UnshowObjectToolPanelItems)}");

            // This script destroys all elements if they exist by their ids.
            UIFileUtils.ExecuteScript(m_UiView, $"{DestroyElementByID("YYTC-tree-age-item")} {DestroyElementByID("YYTC-rotation-row")} {DestroyElementByID("YYTC-prefab-sets-row")} {DestroyElementByID("YYTC-ActivateAgeChange")} {DestroyElementByID("YYTC-ActivatePrefabChange")}");

            // This unregisters the events.
            foreach (BoundEventHandle boundEvent in m_BoundEvents)
            {
                m_UiView.UnregisterFromEvent(boundEvent);
            }

            m_BoundEvents.Clear();

            m_ObjectToolPlacingTree = false;
        }

        /// <summary>
        /// Removes items and unregisters events for Tree controller tool panel.
        /// </summary>
        private void UnshowTreeControllerToolPanel()
        {
            m_Log.Debug($"{nameof(TreeControllerUISystem)}.{nameof(UnshowTreeControllerToolPanel)}");

            // This script removes tree controller panel if it exists.
            UIFileUtils.ExecuteScript(m_UiView, "yyTreeController.panel = document.getElementById(\"tree-controller-panel\"); if (yyTreeController.panel != null) yyTreeController.panel.parentElement.removeChild(yyTreeController.panel);");
            m_ToolIsActive = false;
            foreach (BoundEventHandle boundEvent in m_BoundEvents)
            {
                m_UiView.UnregisterFromEvent(boundEvent);
            }

            m_BoundEvents.Clear();
        }


        /// <summary>
        /// Produces a script for destroying an element by its ID if it exists.
        /// </summary>
        /// <param name="id">ID for element in JS.</param>
        /// <returns>A script for destroying an element by its ID if it exists.</returns>
        private string DestroyElementByID(string id)
        {
            return $"yyTreeController.elementToDestroy = document.getElementById(\"{id}\"); if (yyTreeController.elementToDestroy != null) yyTreeController.elementToDestroy.parentElement.removeChild(yyTreeController.elementToDestroy);";
        }

        /// <summary>
        /// C# event handler for event callback from UI JavaScript.
        /// </summary>
        /// <param name="randomRotation">A bool for whether to randomly rotate trees placed with object tool.</param>
        private void ChangeRandomRotation(bool randomRotation)
        {
            m_TreeObjectDefinitionSystem.RandomRotation = randomRotation;
        }

        /// <summary>
        /// Converts a C# bool to JS string.
        /// </summary>
        /// <param name="flag">a bool.</param>
        /// <returns>"true" or "false"</returns>
        private string BoolToString(bool flag)
        {
            if (flag)
            {
                return "true";
            }

            return "false";
        }

        /// <summary>
        /// C# event handler for event callback from UI JavaScript.
        /// </summary>
        private void ActivatePrefabChange()
        {
            m_Log.Debug("Enable Tool with Prefab Change please.");
            m_ToolSystem.selected = Entity.Null;

            if (m_SelectedPrefabSet == string.Empty && m_ObjectToolSystem.prefab != null)
            {
                m_TreeControllerTool.ClearSelectedTreePrefabs();
                m_TreeControllerTool.SelectTreePrefab(m_ObjectToolSystem.prefab);
            }
            else if (m_SelectedPrefabSet != string.Empty && m_PrefabSetsLookup.ContainsKey(m_SelectedPrefabSet))
            {
                ChangePrefabSet(m_SelectedPrefabSet);
            }

            m_ToolIsActive = false;
            m_ToolSystem.activeTool = m_TreeControllerTool;
        }

        /// <summary>
        /// C# event handler for event callback from UI JavaScript.
        /// </summary>
        /// <param name="ages">An array of bools for whether that age is selected.</param>
        private void ChangeSelectedAges(bool[] ages)
        {
            m_Log.Debug($"{nameof(TreeControllerUISystem)}.{nameof(ChangeSelectedAges)}");
            m_TreeControllerTool.ApplySelectedAges(ages);
        }

        /// <summary>
        /// activates tree controller tool.
        /// </summary>
        private void ActivateTreeControllerTool()
        {
            m_Log.Debug("Enable Tool please.");
            m_SelectedPrefabSet = string.Empty;
            UIFileUtils.ExecuteScript(m_UiView, "yyTreeController.selectedPrefabSet = \"\";");
            m_TreeControllerTool.ClearSelectedTreePrefabs();
            m_ToolIsActive = false;
            m_ToolSystem.selected = Entity.Null;
            m_ToolSystem.activeTool = m_TreeControllerTool;
        }

        /// <summary>
        /// activates object tool plopping trees
        /// </summary>
        private void ActivatePlopTrees()
        {
            m_Log.Debug("Enable Object Tool plopping please.");
            m_ToolSystem.selected = Entity.Null;
            m_ObjectToolSystem.mode = ObjectToolSystem.Mode.Create;
            m_ToolSystem.activeTool = m_ObjectToolSystem;
        }


        /// <summary>
        /// activates tree controller tool.
        /// </summary>
        private void ActivateBrushTrees()
        {
            m_Log.Debug("Enable brushing trees please.");
            m_ToolSystem.selected = Entity.Null;
            m_ObjectToolSystem.mode = ObjectToolSystem.Mode.Brush;
            m_ToolSystem.activeTool = m_ObjectToolSystem;
        }



        /// <summary>
        /// Lots a string from JS.
        /// </summary>
        /// <param name="prefabSetID">ID from button for changing Prefab Set from JS.</param>
        private void ChangePrefabSet(string prefabSetID)
        {
            PrefabBase originallySelectedPrefab = m_TreeControllerTool.GetPrefab();
            m_TreeControllerTool.ClearSelectedTreePrefabs();

            if (!m_PrefabSetsLookup.ContainsKey(prefabSetID))
            {
                m_SelectedPrefabSet = string.Empty;
                m_TreeControllerTool.SelectTreePrefab(originallySelectedPrefab);
                return;
            }

            m_SelectedPrefabSet = prefabSetID;
            foreach (PrefabID id in m_PrefabSetsLookup[prefabSetID])
            {
                if (m_PrefabSystem.TryGetPrefab(id, out PrefabBase prefab))
                {
                    m_TreeControllerTool.SelectTreePrefab(prefab);
                }
            }
        }

        /// <summary>
        /// Logs a string from JS.
        /// </summary>
        /// <param name="log">A string from JS to log.</param>
        private void LogFromJS(string log)
        {
            m_Log.Debug($"{nameof(TreeControllerUISystem)}.{nameof(LogFromJS)} {log}");
        }


        /// <summary>
        /// Changes the radius.
        /// </summary>
        /// <param name="radius">A int from JS for radius.</param>
        private void ChangeRadius(int radius)
        {
            m_TreeControllerTool.Radius = radius;
        }

        /// <summary>
        /// C# event handler for event callback from UI JavaScript.
        /// </summary>
        /// <param name="newToolMode">A string representing the new tool mode.</param>
        private void ChangeToolMode(string newToolMode)
        {
            if (ToolModeLookup.ContainsKey(newToolMode))
            {
                m_TreeControllerTool.SelectionMode = ToolModeLookup[newToolMode];
            }
        }

        /// <summary>
        /// Method implemented by event triggered by tool changing.
        /// </summary>
        /// <param name="tool">The new tool.</param>
        private void OnToolChanged(ToolBaseSystem tool)
        {
            // This script creates the Tree Controller object if it doesn't exist.
            UIFileUtils.ExecuteScript(m_UiView, "if (yyTreeController == null) var yyTreeController = {};");
            m_Log.Debug($"{nameof(TreeControllerUISystem)}.{nameof(OnToolChanged)} {tool.toolID}");

            if (tool.toolID != "Object Tool" && tool.toolID != "Tree Controller Tool")
            {
                if (m_ObjectToolPlacingTree == true)
                {
                    UnshowObjectToolPanelItems();
                }

                if (m_ToolIsActive == true)
                {
                    UnshowTreeControllerToolPanel();
                    UnshowObjectToolPanelItems();
                    UIFileUtils.ExecuteScript(m_UiView, $"{DestroyElementByID("YYTC-tool-mode-item")} {DestroyElementByID("YYTC-selection-mode-item")} {DestroyElementByID("YYTC-radius-row")}");
                }

                if (tool.toolID == "Line Tool")
                {
                    m_Log.Debug($"{nameof(TreeControllerUISystem)}.{nameof(OnToolChanged)} new tool is line tool");
                    m_BoundEvents.Add(m_UiView.RegisterForEvent("YYTC-ChangeSelectedAges", (Action<bool[]>)ChangeSelectedAges));
                    Enabled = true;
                    return;
                }

                Enabled = false;
            }
            else if (tool.toolID == "Object Tool")
            {
                Entity prefabEntity = m_PrefabSystem.GetEntity(m_ObjectToolSystem.prefab);
                if (!EntityManager.HasComponent<TreeData>(prefabEntity) || EntityManager.HasComponent<PlaceholderObjectElement>(prefabEntity))
                {
                    if (m_ObjectToolPlacingTree == true)
                    {
                        UnshowObjectToolPanelItems();
                    }

                    if (m_ToolIsActive == true)
                    {
                        UnshowTreeControllerToolPanel();
                        UnshowObjectToolPanelItems();
                        UIFileUtils.ExecuteScript(m_UiView, $"{DestroyElementByID("YYTC-tool-mode-item")} {DestroyElementByID("YYTC-selection-mode-item")} {DestroyElementByID("YYTC-radius-row")}");
                    }

                    Enabled = false;
                    return;
                }

                Enabled = true;
            }
            else
            {
                Enabled = true;
            }
        }

        /// <summary>
        /// If Panel Anchor is missing thie event is trigger to reset the panel.
        /// </summary>
        private void ResetPanel()
        {
            m_ToolIsActive = false;
            m_ObjectToolPlacingTree = false;
        }

        /// <summary>
        /// Method implemented by event triggered by prefab changing.
        /// </summary>
        /// <param name="prefab">The new prefab.</param>
        private void OnPrefabChanged(PrefabBase prefab)
        {
            m_Log.Debug($"{nameof(TreeControllerUISystem)}.{nameof(OnPrefabChanged)} {prefab.name} {prefab.uiTag}");

            if (m_ToolSystem.activeTool != m_ObjectToolSystem && m_ToolSystem.activeTool != m_TreeControllerTool)
            {
                Enabled = false;
                return;
            }

            Entity prefabEntity = m_PrefabSystem.GetEntity(m_ObjectToolSystem.prefab);
            if (!EntityManager.HasComponent<TreeData>(prefabEntity) || EntityManager.HasComponent<PlaceholderObjectElement>(prefabEntity))
            {
                if (m_ObjectToolPlacingTree == true)
                {
                    m_Log.Debug($"{nameof(TreeControllerUISystem)}.{nameof(OnPrefabChanged)} calling UnShowObjectToolPanelItems");
                    UnshowObjectToolPanelItems();
                }

                if (m_ToolIsActive == true)
                {
                    UnshowTreeControllerToolPanel();
                    UnshowObjectToolPanelItems();
                    UIFileUtils.ExecuteScript(m_UiView, $"{DestroyElementByID("YYTC-tool-mode-item")} {DestroyElementByID("YYTC-selection-mode-item")} {DestroyElementByID("YYTC-radius-row")}");
                }

                Enabled = false;
                return;
            }

            m_TreeControllerTool.ClearSelectedTreePrefabs();
            ResetPrefabSets();

            Enabled = true;
            m_LastObjectToolPrefab = m_ObjectToolSystem.prefab;
        }
    }
}
