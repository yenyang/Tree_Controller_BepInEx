// <copyright file="TreeControllerUISystem.cs" company="Yenyangs Mods. MIT License">
// Copyright (c) Yenyangs Mods. MIT License. All rights reserved.
// </copyright>

// #define VERBOSE
namespace Tree_Controller.Tools
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Windows.Forms;
    using System.Xml.Serialization;
    using cohtml.Net;
    using Colossal.Logging;
    using Colossal.PSI.Environment;
    using Game.Prefabs;
    using Game.SceneFlow;
    using Game.Tools;
    using Game.UI;
    using Game.UI.Localization;
    using Tree_Controller.Settings;
    using Tree_Controller.Systems;
    using Tree_Controller.Utils;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;
    using static Tree_Controller.Tools.TreeControllerTool;

    /// <summary>
    /// UI system for Object Tool while using tree prefabs.
    /// </summary>
    public partial class TreeControllerUISystem : UISystemBase
    {
        private readonly List<PrefabID> m_VanillaDeciduousPrefabIDs = new ()
        {
            { new PrefabID("StaticObjectPrefab", "EU_AlderTree01") },
            { new PrefabID("StaticObjectPrefab", "BirchTree01") },
            { new PrefabID("StaticObjectPrefab", "NA_LondonPlaneTree01") },
            { new PrefabID("StaticObjectPrefab", "NA_LindenTree01") },
            { new PrefabID("StaticObjectPrefab", "NA_HickoryTree01") },
            { new PrefabID("StaticObjectPrefab", "EU_ChestnutTree01") },
            { new PrefabID("StaticObjectPrefab", "OakTree01") },
        };

        private readonly List<PrefabID> m_VanillaEvergreenPrefabIDs = new ()
        {
            { new PrefabID("StaticObjectPrefab", "PineTree01") },
            { new PrefabID("StaticObjectPrefab", "SpruceTree01") },
        };

        private readonly List<PrefabID> m_VanillaWildBushPrefabs = new ()
        {
            { new PrefabID("StaticObjectPrefab", "GreenBushWild01") },
            { new PrefabID("StaticObjectPrefab", "GreenBushWild02") },
            { new PrefabID("StaticObjectPrefab", "FlowerBushWild01") },
            { new PrefabID("StaticObjectPrefab", "FlowerBushWild02") },
        };

        private readonly List<PrefabID> m_DefaultCustomSet1Prefabs = new ()
        {
            { new PrefabID("StaticObjectPrefab", "NA_LondonPlaneTree01") },
            { new PrefabID("StaticObjectPrefab", "NA_LindenTree01") },
            { new PrefabID("StaticObjectPrefab", "NA_HickoryTree01") },
        };

        private readonly List<PrefabID> m_DefaultCustomSet2Prefabs = new ()
        {
            { new PrefabID("StaticObjectPrefab", "EU_AlderTree01") },
            { new PrefabID("StaticObjectPrefab", "EU_ChestnutTree01") },
            { new PrefabID("StaticObjectPrefab", "EU_PoplarTree01") },
        };

        private readonly List<PrefabID> m_DefaultCustomSet3Prefabs = new ()
        {
            { new PrefabID("StaticObjectPrefab", "BirchTree01") },
            { new PrefabID("StaticObjectPrefab", "OakTree01") },
            { new PrefabID("StaticObjectPrefab", "AppleTree01") },
        };

        private readonly List<PrefabID> m_DefaultCustomSet4Prefabs = new ()
        {
            { new PrefabID("StaticObjectPrefab", "BirchTree01") },
            { new PrefabID("StaticObjectPrefab", "EU_PoplarTree01") },
        };

        private readonly List<PrefabID> m_DefaultCustomSet5Prefabs = new ()
        {
            { new PrefabID("StaticObjectPrefab", "EU_ChestnutTree01") },
            { new PrefabID("StaticObjectPrefab", "NA_LondonPlaneTree01") },
            { new PrefabID("StaticObjectPrefab", "NA_LindenTree01") },
        };

        private readonly Dictionary<string, TCSelectionMode> ToolModeLookup = new ()
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

        private cohtml.Net.View m_UiView;
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
        private string m_InjectedCSS = string.Empty;
        private bool m_ToolIsActive;
        private bool m_ObjectToolPlacingTree = false;
        private string m_SelectedPrefabSet = string.Empty;
        private bool m_PreviousPanelsCleared = false;
        private bool m_FirstTimeInjectingJS = true;
        private string m_ContentFolder;
        private bool m_UpdateSelectionSet = false;
        private int m_FrameCount = 0;
        private bool m_MultiplePrefabsSelected = false;
        private EntityQuery m_VegetationQuery;

        /// <summary>
        /// Gets or sets a value indicating whether the selection set of buttons on the Toolbar UI needs to be updated.
        /// </summary>
        public bool UpdateSelectionSet
        {
            get => m_UpdateSelectionSet;
            set => m_UpdateSelectionSet = value;
        }

        /// <summary>
        /// Resets the selected prefab set.
        /// </summary>
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

        /// <summary>
        /// Adds selected to the selected prefab.
        /// </summary>
        /// <param name="prefab">The selected prefab.</param>
        public void SelectPrefab(PrefabBase prefab)
        {
            if (prefab == null)
            {
                return;
            }

            // This script creates the Tree Controller object if it doesn't exist.
            UIFileUtils.ExecuteScript(m_UiView, "if (yyTreeController == null) var yyTreeController = {};");

            // This script searches through all img and adds selected if the src of that image contains the name of the prefab.
            UIFileUtils.ExecuteScript(m_UiView, $"yyTreeController.tagElements = document.getElementsByTagName(\"img\"); for (yyTreeController.i = 0; yyTreeController.i < yyTreeController.tagElements.length; yyTreeController.i++) {{ if (yyTreeController.tagElements[yyTreeController.i].src.includes(\"{prefab.name}\")) {{ yyTreeController.tagElements[yyTreeController.i].parentNode.classList.add(\"selected\");  }} }} ");
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
            m_ContentFolder = Path.Combine(EnvPath.kUserDataPath, "ModsData", "Mods_Yenyang_Tree_Controller", "CustomSets");
            System.IO.Directory.CreateDirectory(m_ContentFolder);
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
                { "YYTC-custom-set-1", m_DefaultCustomSet1Prefabs },
                { "YYTC-custom-set-2", m_DefaultCustomSet2Prefabs },
                { "YYTC-custom-set-3", m_DefaultCustomSet3Prefabs },
                { "YYTC-custom-set-4", m_DefaultCustomSet4Prefabs },
                { "YYTC-custom-set-5", m_DefaultCustomSet5Prefabs },
            };

            for (int i = 1; i <= 5; i++)
            {
                TryLoadCustomPrefabSet($"YYTC-custom-set-{i}");
            }

            m_InjectedJS = UIFileUtils.ReadJS(Path.Combine(UIFileUtils.AssemblyPath, "ui.js"));
            m_TreeControllerPanelScript = UIFileUtils.ReadHTML(Path.Combine(UIFileUtils.AssemblyPath, "YYTC-selection-mode-item.html"), "if (document.getElementById(\"tree-controller-panel\") == null) { yyTreeController.div.className = \"tool-options-panel_Se6\"; yyTreeController.div.id = \"tree-controller-panel\"; yyTreeController.ToolColumns = document.getElementsByClassName(\"tool-side-column_l9i\"); if (yyTreeController.ToolColumns[0] != null) yyTreeController.ToolColumns[0].appendChild(yyTreeController.div); if (typeof yyTreeController.setupYYTCSelectionModeItem == 'function') yyTreeController.setupYYTCSelectionModeItem(); }");
            m_SelectionRowItemScript = UIFileUtils.ReadHTML(Path.Combine(UIFileUtils.AssemblyPath, "YYTC-selection-mode-content.html"), "if (document.getElementById(\"YYTC-selection-mode-item\") == null) { yyTreeController.div.className = \"item_bZY\"; yyTreeController.div.id = \"YYTC-selection-mode-item\"; yyTreeController.AgesElement = document.getElementById(\"YYTC-tree-age-item\"); if (yyTreeController.AgesElement != null) yyTreeController.AgesElement.insertAdjacentElement('afterend', yyTreeController.div);if (typeof yyTreeController.setupYYTCSelectionModeItem == 'function') yyTreeController.setupYYTCSelectionModeItem(); }");
            m_ToolModeItemScript = UIFileUtils.ReadHTML(Path.Combine(UIFileUtils.AssemblyPath, "YYTC-tool-mode-content.html"), "if (document.getElementById(\"YYTC-tool-mode-item\") == null) { yyTreeController.div.className = \"item_bZY\"; yyTreeController.div.id =\"YYTC-tool-mode-item\"; yyTreeController.OptionsPanels = document.getElementsByClassName(\"tool-options-panel_Se6\"); if (yyTreeController.OptionsPanels[0] != null) { yyTreeController.OptionsPanels[0].appendChild(yyTreeController.div); if (typeof yyTreeController.setupToolModeButtons == 'function') yyTreeController.setupToolModeButtons(); } }");
            m_AgeChangingToolModeItemScript = UIFileUtils.ReadHTML(Path.Combine(UIFileUtils.AssemblyPath, "YYTC-tool-mode-content.html"), "if (document.getElementById(\"YYTC-tool-mode-item\") == null) { yyTreeController.div.className = \"item_bZY\"; yyTreeController.div.id =\"YYTC-tool-mode-item\"; yyTreeController.panel = document.getElementById(\"tree-controller-panel\"); if (yyTreeController.panel != null) { yyTreeController.panel.appendChild(yyTreeController.div); if (typeof yyTreeController.setupToolModeButtons == 'function') yyTreeController.setupToolModeButtons(); } }");
            m_InjectedCSS = UIFileUtils.ReadCSS(Path.Combine(UIFileUtils.AssemblyPath, "ui.css"));

            if (m_UiView == null)
            {
                m_Log.Warn($"{nameof(TreeControllerUISystem)}.{nameof(OnCreate)} m_UiView == null");
            }

            m_VegetationQuery = GetEntityQuery(ComponentType.ReadOnly<Vegetation>());

            m_Log.Info($"{nameof(TreeControllerUISystem)}.{nameof(OnCreate)}");
            Enabled = false;
            base.OnCreate();
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            List<PrefabBase> selectedPrefabs = m_TreeControllerTool.GetSelectedPrefabs();
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

                        if (m_MultiplePrefabsSelected == false && m_TreeControllerTool.GetSelectedPrefabs().Count > 1)
                        {
                            m_UpdateSelectionSet = true;
                        }

                        if (m_UpdateSelectionSet && m_FrameCount <= 5)
                        {
                            UnselectPrefabs();

                            foreach (PrefabBase prefab in selectedPrefabs)
                            {
                                SelectPrefab(prefab);
                            }

                            if (selectedPrefabs.Count > 1)
                            {
                                m_MultiplePrefabsSelected = true;
                            }
                            else
                            {
                                m_MultiplePrefabsSelected = false;
                            }

                            if (m_FrameCount == 5)
                            {
                                m_UpdateSelectionSet = false;
                                m_FrameCount = 6;
                            }
                            else
                            {
                                m_FrameCount++;
                            }
                        }
                        else if (m_UpdateSelectionSet)
                        {
                            if (m_FrameCount == 6)
                            {
                                m_FrameCount = 0;
                            }

                            m_FrameCount++;
                        }
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

                if (m_InjectedJS == string.Empty)
                {
                    m_Log.Warn($"{nameof(TreeControllerUISystem)}.{nameof(OnUpdate)} m_InjectedJS is empty!!! Did you forget to include the ui.js file in the mod install folder?");
                    m_InjectedJS = UIFileUtils.ReadJS(Path.Combine(UIFileUtils.AssemblyPath, "ui.js"));
                    m_TreeControllerPanelScript = UIFileUtils.ReadHTML(Path.Combine(UIFileUtils.AssemblyPath, "YYTC-selection-mode-item.html"), "if (document.getElementById(\"tree-controller-panel\") == null) { yyTreeController.div.className = \"tool-options-panel_Se6\"; yyTreeController.div.id = \"tree-controller-panel\"; yyTreeController.ToolColumns = document.getElementsByClassName(\"tool-side-column_l9i\"); if (yyTreeController.ToolColumns[0] != null) yyTreeController.ToolColumns[0].appendChild(yyTreeController.div); if (typeof yyTreeController.setupYYTCSelectionModeItem == 'function') yyTreeController.setupYYTCSelectionModeItem(); }");
                    m_SelectionRowItemScript = UIFileUtils.ReadHTML(Path.Combine(UIFileUtils.AssemblyPath, "YYTC-selection-mode-content.html"), "if (document.getElementById(\"YYTC-selection-mode-item\") == null) { yyTreeController.div.className = \"item_bZY\"; yyTreeController.div.id = \"YYTC-selection-mode-item\"; yyTreeController.AgesElement = document.getElementById(\"YYTC-tree-age-item\"); if (yyTreeController.AgesElement != null) yyTreeController.AgesElement.insertAdjacentElement('afterend', yyTreeController.div);if (typeof yyTreeController.setupYYTCSelectionModeItem == 'function') yyTreeController.setupYYTCSelectionModeItem(); }");
                    m_ToolModeItemScript = UIFileUtils.ReadHTML(Path.Combine(UIFileUtils.AssemblyPath, "YYTC-tool-mode-content.html"), "if (document.getElementById(\"YYTC-tool-mode-item\") == null) { yyTreeController.div.className = \"item_bZY\"; yyTreeController.div.id =\"YYTC-tool-mode-item\"; yyTreeController.OptionsPanels = document.getElementsByClassName(\"tool-options-panel_Se6\"); if (yyTreeController.OptionsPanels[0] != null) { yyTreeController.OptionsPanels[0].appendChild(yyTreeController.div); if (typeof yyTreeController.setupToolModeButtons == 'function') yyTreeController.setupToolModeButtons(); } }");
                    m_AgeChangingToolModeItemScript = UIFileUtils.ReadHTML(Path.Combine(UIFileUtils.AssemblyPath, "YYTC-tool-mode-content.html"), "if (document.getElementById(\"YYTC-tool-mode-item\") == null) { yyTreeController.div.className = \"item_bZY\"; yyTreeController.div.id =\"YYTC-tool-mode-item\"; yyTreeController.panel = document.getElementById(\"tree-controller-panel\"); if (yyTreeController.panel != null) { yyTreeController.panel.appendChild(yyTreeController.div); if (typeof yyTreeController.setupToolModeButtons == 'function') yyTreeController.setupToolModeButtons(); } }");
                    m_InjectedCSS = UIFileUtils.ReadCSS(Path.Combine(UIFileUtils.AssemblyPath, "ui.css"));
                }

                bool[] ages = m_TreeControllerTool.GetSelectedAges();

                // This script injects all the css styles.
                UIFileUtils.ExecuteScript(m_UiView, m_InjectedCSS);

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

                // If this is the first time injecting the JS, wait a frame to ensure it has time to setup the functions.
                if (m_FirstTimeInjectingJS)
                {
                    m_FirstTimeInjectingJS = false;
                    return;
                }

                m_BoundEvents.Add(m_UiView.RegisterForEvent("TClog", (Action<string>)LogFromJS));

                if (m_TreeControllerTool.GetPrefab() == null) // Tree age changing only.
                {
                    // This script builds the Tool mode item row, tree age item row. And checks if radius row needs to be added.
                    UIFileUtils.ExecuteScript(m_UiView, $"{m_TreeControllerPanelScript} yyTreeController.selectionModeItem = document.getElementById(\"YYTC-selection-mode-item\"); if (yyTreeController.selectionModeItem != null && typeof yyTreeController.buildTreeAgeItem == 'function')  yyTreeController.buildTreeAgeItem(yyTreeController.selectionModeItem,'afterend'); if (typeof yyTreeController.checkIfNeedToBuildRadius == 'function') yyTreeController.checkIfNeedToBuildRadius();");

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
                    UIFileUtils.ExecuteScript(m_UiView, "yyTreeController.entities = document.getElementsByClassName(\"tool-options-panel_Se6\"); if (yyTreeController.entities[0] != null) { if (yyTreeController.entities[0].firstChild != null && typeof yyTreeController.buildTreeAgeItem == 'function') { yyTreeController.buildTreeAgeItem(yyTreeController.entities[0].firstChild, 'beforebegin'); } }");

                    // This script builds the Tool mode item row, tree age item row. And checks if radius row needs to be added.
                    UIFileUtils.ExecuteScript(m_UiView, $"{m_SelectionRowItemScript} if (typeof yyTreeController.checkIfNeedToBuildRadius == 'function') yyTreeController.checkIfNeedToBuildRadius();");

                    // This scripts builds the prefab sets row.
                    UIFileUtils.ExecuteScript(m_UiView, $"yyTreeController.ageItem = document.getElementById(\"YYTC-tree-age-item\"); if (yyTreeController.ageItem != null && typeof yyTreeController.buildPrefabSetsRow == 'function') yyTreeController.buildPrefabSetsRow(yyTreeController.ageItem,'afterend');");

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

            if (m_ObjectToolPlacingTree && !m_ObjectToolSystem.brushing)
            {
                UIFileUtils.ExecuteScript(m_UiView, $"if (document.getElementById(\"YYTC-rotation-row\") == null) engine.trigger('YYTC-rotation-row-missing');");
            }
            else if (m_ObjectToolPlacingTree)
            {
                UIFileUtils.ExecuteScript(m_UiView, $"if (document.getElementById(\"YYTC-prefab-sets-row\") == null) engine.trigger('YYTC-prefab-sets-row-missing');");
            }

            // This script creates the Tree Controller object if it doesn't exist.
            UIFileUtils.ExecuteScript(m_UiView, "if (yyTreeController == null) var yyTreeController = {};");

            // This script looks for any img srcs that use brush and then adds new Change tool buttons to the grandparent element.
            UIFileUtils.ExecuteScript(m_UiView, $"if (document.getElementById(\"YYTC-ActivateAgeChange\") == null) {{ yyTreeController.tagElements = document.getElementsByTagName(\"img\");  for (yyTreeController.i = 0; yyTreeController.i < yyTreeController.tagElements.length; yyTreeController.i++) {{  if (yyTreeController.tagElements[yyTreeController.i].src == \"Media/Tools/Object%20Tool/Brush.svg\" && typeof yyTreeController.buildActivateToolButton == 'function') {{  yyTreeController.buildActivateToolButton(\"YYTC-ActivateAgeChange\", \"coui://uil/Standard/ReplaceTreeAge.svg\", yyTreeController.tagElements[yyTreeController.i].parentNode.parentNode, \"change-age-tool\");    yyTreeController.buildActivateToolButton(\"YYTC-ActivatePrefabChange\", \"coui://uil/Standard/Replace.svg\", yyTreeController.tagElements[yyTreeController.i].parentNode.parentNode, \"change-prefab-tool\");  }} }} }}");

            if (m_ObjectToolPlacingTree == false)
            {
                bool[] ages = m_TreeControllerTool.GetSelectedAges();

                if (m_InjectedJS == string.Empty)
                {
                    m_Log.Warn($"{nameof(TreeControllerUISystem)}.{nameof(OnUpdate)} m_InjectedJS is empty!!! Did you forget to include the ui.js file in the mod install folder?");
                    m_InjectedJS = UIFileUtils.ReadJS(Path.Combine(UIFileUtils.AssemblyPath, "ui.js"));
                    m_TreeControllerPanelScript = UIFileUtils.ReadHTML(Path.Combine(UIFileUtils.AssemblyPath, "YYTC-selection-mode-item.html"), "if (document.getElementById(\"tree-controller-panel\") == null) { yyTreeController.div.className = \"tool-options-panel_Se6\"; yyTreeController.div.id = \"tree-controller-panel\"; yyTreeController.ToolColumns = document.getElementsByClassName(\"tool-side-column_l9i\"); if (yyTreeController.ToolColumns[0] != null) yyTreeController.ToolColumns[0].appendChild(yyTreeController.div); if (typeof yyTreeController.setupYYTCSelectionModeItem == 'function') yyTreeController.setupYYTCSelectionModeItem(); }");
                    m_SelectionRowItemScript = UIFileUtils.ReadHTML(Path.Combine(UIFileUtils.AssemblyPath, "YYTC-selection-mode-content.html"), "if (document.getElementById(\"YYTC-selection-mode-item\") == null) { yyTreeController.div.className = \"item_bZY\"; yyTreeController.div.id = \"YYTC-selection-mode-item\"; yyTreeController.AgesElement = document.getElementById(\"YYTC-tree-age-item\"); if (yyTreeController.AgesElement != null) yyTreeController.AgesElement.insertAdjacentElement('afterend', yyTreeController.div);if (typeof yyTreeController.setupYYTCSelectionModeItem == 'function') yyTreeController.setupYYTCSelectionModeItem(); }");
                    m_ToolModeItemScript = UIFileUtils.ReadHTML(Path.Combine(UIFileUtils.AssemblyPath, "YYTC-tool-mode-content.html"), "if (document.getElementById(\"YYTC-tool-mode-item\") == null) { yyTreeController.div.className = \"item_bZY\"; yyTreeController.div.id =\"YYTC-tool-mode-item\"; yyTreeController.OptionsPanels = document.getElementsByClassName(\"tool-options-panel_Se6\"); if (yyTreeController.OptionsPanels[0] != null) { yyTreeController.OptionsPanels[0].appendChild(yyTreeController.div); if (typeof yyTreeController.setupToolModeButtons == 'function') yyTreeController.setupToolModeButtons(); } }");
                    m_AgeChangingToolModeItemScript = UIFileUtils.ReadHTML(Path.Combine(UIFileUtils.AssemblyPath, "YYTC-tool-mode-content.html"), "if (document.getElementById(\"YYTC-tool-mode-item\") == null) { yyTreeController.div.className = \"item_bZY\"; yyTreeController.div.id =\"YYTC-tool-mode-item\"; yyTreeController.panel = document.getElementById(\"tree-controller-panel\"); if (yyTreeController.panel != null) { yyTreeController.panel.appendChild(yyTreeController.div); if (typeof yyTreeController.setupToolModeButtons == 'function') yyTreeController.setupToolModeButtons(); } }");
                    m_InjectedCSS = UIFileUtils.ReadCSS(Path.Combine(UIFileUtils.AssemblyPath, "ui.css"));
                }

                // This script injects all the css styles.
                UIFileUtils.ExecuteScript(m_UiView, m_InjectedCSS);

                // This script sets the ages map variable in JS.
                UIFileUtils.ExecuteScript(m_UiView, $"yyTreeController.selectedAges = new Map([[\"YYTC-child\", {BoolToString(ages[0])}],[\"YYTC-teen\", {BoolToString(ages[1])}],[\"YYTC-adult\", {BoolToString(ages[2])}],[\"YYTC-elderly\", {BoolToString(ages[3])}],[\"YYTC-dead\", {BoolToString(ages[4])}],]);");

                // This script sets the selected prefab set variable in JS.
                UIFileUtils.ExecuteScript(m_UiView, $"yyTreeController.selectedPrefabSet = \"{m_SelectedPrefabSet}\";");

                // This scrip sets the random rotation variable in JS.
                UIFileUtils.ExecuteScript(m_UiView, $"yyTreeController.randomRotation = {BoolToString(TreeControllerMod.Settings.RandomRotation)};");

                // This script defines all the JS functions if they do not exist.
                UIFileUtils.ExecuteScript(m_UiView, m_InjectedJS);

                // If this is the first time injecting the JS, wait a frame to ensure it has time to setup the functions.
                if (m_FirstTimeInjectingJS)
                {
                    m_FirstTimeInjectingJS = false;
                    return;
                }

                foreach (PrefabBase prefabBase in selectedPrefabs)
                {
                    if (m_PrefabSystem.TryGetEntity(prefabBase, out Entity prefabEntity))
                    {
                        if (EntityManager.HasComponent<TreeData>(prefabEntity))
                        {
                            // This script builds Tree Age item used for brushing trees.
                            UIFileUtils.ExecuteScript(m_UiView, "yyTreeController.entities = document.getElementsByClassName(\"tool-options-panel_Se6\"); if (yyTreeController.entities[0] != null) { if (yyTreeController.entities[0].firstChild != null && typeof yyTreeController.buildTreeAgeItem == 'function') { yyTreeController.buildTreeAgeItem(yyTreeController.entities[0].firstChild, 'beforebegin'); } }");
                            break;
                        }
                    }
                }

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

                if (!m_ObjectToolSystem.brushing)
                {
                    m_BoundEvents.Add(m_UiView.RegisterForEvent("YYTC-rotation-row-missing", (Action)ResetPanel));
                }
                else
                {
                    m_BoundEvents.Add(m_UiView.RegisterForEvent("YYTC-prefab-sets-row-missing", (Action)ResetPanel));
                }

                m_ObjectToolPlacingTree = true;
            }

            if (m_ObjectToolSystem.brushing)
            {
                // This script destroys rotation and plop age row while not plopping single tree.
                UIFileUtils.ExecuteScript(m_UiView, "yyTreeController.destroyElementByID(\"YYTC-rotation-row\");");

                // This script buils the prefab Sets row for brushing sets of trees.
                UIFileUtils.ExecuteScript(m_UiView, "yyTreeController.ageRow = document.getElementById(\"YYTC-tree-age-item\"); if (yyTreeController.ageRow != null && typeof yyTreeController.buildPrefabSetsRow == 'function') { yyTreeController.buildPrefabSetsRow(yyTreeController.ageRow, 'afterend'); }");

                if (m_MultiplePrefabsSelected == false && selectedPrefabs.Count > 1)
                {
                    m_UpdateSelectionSet = true;
                }

                if (m_MultiplePrefabsSelected && m_ObjectToolSystem.prefab != m_LastObjectToolPrefab && !selectedPrefabs.Contains(m_ObjectToolSystem.prefab))
                {
                    UnselectPrefabs();
                    m_TreeControllerTool.ClearSelectedTreePrefabs();
                    m_MultiplePrefabsSelected = false;
                    ResetPrefabSets();
                    m_Log.Debug($"{nameof(TreeControllerUISystem)}.{nameof(OnUpdate)} selectionSet Reset due to prefab changing without toggling OnPrefabChanged");
                }

                if (m_UpdateSelectionSet && m_FrameCount <= 5)
                {
                    UnselectPrefabs();

                    foreach (PrefabBase prefab in selectedPrefabs)
                    {
                        SelectPrefab(prefab);
                    }

                    if (selectedPrefabs.Count > 1)
                    {
                        m_MultiplePrefabsSelected = true;
                    }
                    else
                    {
                        m_MultiplePrefabsSelected = false;
                    }

                    if (m_FrameCount == 5)
                    {
                        m_UpdateSelectionSet = false;
                        m_FrameCount = 6;
                    }
                    else
                    {
                        m_FrameCount++;
                    }
                }
                else if (m_UpdateSelectionSet)
                {
                    if (m_FrameCount == 6)
                    {
                        m_FrameCount = 0;
                    }

                    m_FrameCount++;
                }
            }
            else
            {
                // This script destroys the tree age item row and prefab sets rows that were used for brushing trees.
                UIFileUtils.ExecuteScript(m_UiView, "yyTreeController.destroyElementByID(\"YYTC-prefab-sets-row\");");

                // This script builds Tree Age item used for brushing trees.
                UIFileUtils.ExecuteScript(m_UiView, "yyTreeController.entities = document.getElementsByClassName(\"tool-options-panel_Se6\"); if (yyTreeController.entities[0] != null) { if (yyTreeController.entities[0].firstChild != null && typeof yyTreeController.buildRotationRow == 'function') { yyTreeController.buildRotationRow(yyTreeController.entities[0].firstChild, 'afterend'); } }");

                if (m_MultiplePrefabsSelected)
                {
                    UnselectPrefabs();
                }
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
            TreeControllerMod.Settings.RandomRotation = randomRotation;
            TreeControllerMod.Settings.ApplyAndSave();
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

            if (m_SelectedPrefabSet == string.Empty && m_ObjectToolSystem.prefab != null && m_TreeControllerTool.GetSelectedPrefabs().Count <= 1)
            {
                UnselectPrefabs();
                m_TreeControllerTool.ClearSelectedTreePrefabs();
                m_TreeControllerTool.SelectTreePrefab(m_ObjectToolSystem.prefab);
            }
            else if (m_SelectedPrefabSet != string.Empty && m_PrefabSetsLookup.ContainsKey(m_SelectedPrefabSet))
            {
                ChangePrefabSet(m_SelectedPrefabSet);
            }

            m_UpdateSelectionSet = true;
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
            UnselectPrefabs();
            m_MultiplePrefabsSelected = false;
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
            m_UpdateSelectionSet = true;
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

            if (!m_PrefabSetsLookup.ContainsKey(prefabSetID))
            {
                UnselectPrefabs();
                m_TreeControllerTool.ClearSelectedTreePrefabs();
                m_SelectedPrefabSet = string.Empty;
                m_TreeControllerTool.SelectTreePrefab(originallySelectedPrefab);
                return;
            }

            List<PrefabBase> selectedPrefabs = m_TreeControllerTool.GetSelectedPrefabs();
            if (prefabSetID.Contains("custom") && selectedPrefabs.Count > 1 && (Control.ModifierKeys & Keys.Control) == Keys.Control)
            {
                m_Log.Debug($"{nameof(TreeControllerUISystem)}.{nameof(ChangePrefabSet)} trying to add prefab ids to set lookup.");
                TrySaveCustomPrefabSet(prefabSetID, selectedPrefabs);
            }

            if (m_PrefabSetsLookup[prefabSetID].Count == 0)
            {
                m_SelectedPrefabSet = string.Empty;
                m_TreeControllerTool.SelectTreePrefab(originallySelectedPrefab);
                UIFileUtils.ExecuteScript(m_UiView, $"yyTreeController.prefabSet = document.getElementById(\"{prefabSetID}\"); if (yyTreeController.prefabSet) yyTreeController.prefabSet.classList.remove(\"selected\");");
                m_Log.Warn($"{nameof(TreeControllerUISystem)}.{nameof(ChangePrefabSet)} could not select empty set");
                return;
            }

            if (!m_PrefabSetsLookup[prefabSetID].Contains(m_ObjectToolSystem.prefab.GetPrefabID()) && m_ToolSystem.activeTool == m_ObjectToolSystem)
            {
                // This script searches through all img and adds removes selected if the src of that image contains the name of the prefab and is the active prefab.
                UIFileUtils.ExecuteScript(m_UiView, $"yyTreeController.tagElements = document.getElementsByTagName(\"img\"); for (yyTreeController.i = 0; yyTreeController.i < yyTreeController.tagElements.length; yyTreeController.i++) {{ if (yyTreeController.tagElements[yyTreeController.i].src.includes(\"{m_ObjectToolSystem.prefab.name}\")) {{ yyTreeController.tagElements[yyTreeController.i].parentNode.classList.remove(\"selected\");  }} }} ");
            }

            UnselectPrefabs();
            m_TreeControllerTool.ClearSelectedTreePrefabs();
            m_SelectedPrefabSet = prefabSetID;
            foreach (PrefabID id in m_PrefabSetsLookup[prefabSetID])
            {
                if (m_PrefabSystem.TryGetPrefab(id, out PrefabBase prefab))
                {
                    m_TreeControllerTool.SelectTreePrefab(prefab);
                    SelectPrefab(prefab);
                    m_MultiplePrefabsSelected = true;
                }
            }
        }

        private void UnselectPrefabs()
        {
            // This script creates the Tree Controller object if it doesn't exist.
            UIFileUtils.ExecuteScript(m_UiView, "if (yyTreeController == null) var yyTreeController = {};");
            NativeList<Entity> m_VegetationPrefabEntities = m_VegetationQuery.ToEntityListAsync(Allocator.Temp, out JobHandle jobHandle);
            jobHandle.Complete();
            foreach (Entity e in m_VegetationPrefabEntities)
            {
                if (m_PrefabSystem.TryGetPrefab(e, out PrefabBase prefab))
                {
                    if (prefab != m_ToolSystem.activePrefab)
                    {
                        // This script searches through all img and adds removes selected if the src of that image contains the name of the prefab and is not the active prefab.
                        UIFileUtils.ExecuteScript(m_UiView, $"yyTreeController.tagElements = document.getElementsByTagName(\"img\"); for (yyTreeController.i = 0; yyTreeController.i < yyTreeController.tagElements.length; yyTreeController.i++) {{ if (yyTreeController.tagElements[yyTreeController.i].src.includes(\"{prefab.name}\")) {{ yyTreeController.tagElements[yyTreeController.i].parentNode.classList.remove(\"selected\");  }} }} ");
                    }
                }
            }

            m_VegetationPrefabEntities.Dispose();
            m_MultiplePrefabsSelected = false;
            m_Log.Debug($"{nameof(TreeControllerUISystem)}.{nameof(UnselectPrefabs)}");
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
                UnselectPrefabs();

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
                if (m_PrefabSystem.TryGetEntity(m_ObjectToolSystem.prefab, out Entity prefabEntity))
                {
                    if (!EntityManager.HasComponent<Vegetation>(prefabEntity))
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

                    m_LastObjectToolPrefab = m_ObjectToolSystem.prefab;
                    m_TreeControllerTool.SelectTreePrefab(m_ObjectToolSystem.prefab);
                    m_Log.Debug($"{nameof(TreeControllerUISystem)}.{nameof(OnToolChanged)} selected {m_ObjectToolSystem.prefab.name}");

                    Enabled = true;
                }
                else
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

            if (m_ObjectToolSystem.prefab != null && m_PrefabSystem.TryGetEntity(m_ObjectToolSystem.prefab, out Entity prefabEntity))
            {
                if (!EntityManager.HasComponent<Vegetation>(prefabEntity))
                {
                    UnselectPrefabs();

                    if (m_ObjectToolPlacingTree == true)
                    {
                        m_Log.Debug($"{nameof(TreeControllerUISystem)}.{nameof(OnPrefabChanged)} calling UnShowObjectToolPanelItems");
                        UnshowObjectToolPanelItems();
                        m_TreeControllerTool.ClearSelectedTreePrefabs();
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
            }
            else
            {
                UnselectPrefabs();

                if (m_ObjectToolPlacingTree == true)
                {
                    m_Log.Debug($"{nameof(TreeControllerUISystem)}.{nameof(OnPrefabChanged)} calling UnShowObjectToolPanelItems");
                    UnshowObjectToolPanelItems();
                    m_TreeControllerTool.ClearSelectedTreePrefabs();
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
            ResetPrefabSets();
            if (m_ToolSystem.activeTool == m_TreeControllerTool || m_ObjectToolSystem.brushing)
            {
                if ((Control.ModifierKeys & Keys.Control) != Keys.Control)
                {
                    UnselectPrefabs();
                }
            }

            List<PrefabBase> selectedPrefabs = m_TreeControllerTool.GetSelectedPrefabs();
            bool treeSelected = false;
            foreach (PrefabBase prefabBase in selectedPrefabs)
            {
                if (m_PrefabSystem.TryGetEntity(prefabBase, out Entity currentPrefabEntity))
                {
                    if (EntityManager.HasComponent<TreeData>(currentPrefabEntity))
                    {
                        UIFileUtils.ExecuteScript(m_UiView, $"if (document.getElementById(\"YYTC-tree-age-item\") == null) engine.trigger('YYTC-tree-age-item-missing');");
                        treeSelected = true;
                        break;
                    }
                }
            }

            if (!treeSelected)
            {
                UIFileUtils.ExecuteScript(m_UiView, DestroyElementByID("YYTC-tree-age-item"));
            }

            if (m_ObjectToolSystem.prefab != null)
            {
                m_LastObjectToolPrefab = m_ObjectToolSystem.prefab;
            }
        }

        private bool TrySaveCustomPrefabSet(string prefabSetID, List<PrefabBase> prefabBases)
        {
            List<PrefabID> prefabIDs = new List<PrefabID>();
            foreach (PrefabBase prefab in prefabBases)
            {
                prefabIDs.Add(prefab.GetPrefabID());
            }

            return TrySaveCustomPrefabSet(prefabSetID, prefabIDs);
        }

        private bool TrySaveCustomPrefabSet(string prefabSetID, List<PrefabID> prefabIDs)
        {
            string fileName = Path.Combine(m_ContentFolder, $"{prefabSetID}.xml");
            CustomSetRepository repository = new (
                name: LocalizedString.Id($"YY_TREE_CONTROLLER.{prefabSetID}").value,
                nameLocaleKey: $"YY_TREE_CONTROLLER.{prefabSetID}",
                description: LocalizedString.Id($"YY_TREE_CONTROLLER_DESCRIPTION.{prefabSetID}").value,
                descriptionLocaleKey: $"YY_TREE_CONTROLLER_DESCRIPTION.{prefabSetID}",
                customSet: prefabIDs);

            m_PrefabSetsLookup[prefabSetID].Clear();
            foreach (PrefabID prefab in prefabIDs)
            {
                m_PrefabSetsLookup[prefabSetID].Add(prefab);
            }

            try
            {
                XmlSerializer serTool = new XmlSerializer(typeof(CustomSetRepository)); // Create serializer
                using (System.IO.FileStream file = System.IO.File.Create(fileName)) // Create file
                {
                    serTool.Serialize(file, repository); // Serialize whole properties
                }

                return true;
            }
            catch (Exception ex)
            {
                m_Log.Warn($"{nameof(TreeControllerUISystem)}.{nameof(TrySaveCustomPrefabSet)} Could not save values for {prefabSetID}. Encountered exception {ex}");
                return false;
            }
        }

        private bool TryLoadCustomPrefabSet(string prefabSetID)
        {
            string fileName = Path.Combine(m_ContentFolder, $"{prefabSetID}.xml");
            if (File.Exists(fileName))
            {
                try
                {
                    XmlSerializer serTool = new XmlSerializer(typeof(CustomSetRepository)); // Create serializer
                    using System.IO.FileStream readStream = new System.IO.FileStream(fileName, System.IO.FileMode.Open); // Open file
                    CustomSetRepository result = (CustomSetRepository)serTool.Deserialize(readStream); // Des-serialize to new Properties

                    if (m_PrefabSetsLookup.ContainsKey(prefabSetID) && result.GetPrefabIDs().Count > 0)
                    {
                        m_PrefabSetsLookup[prefabSetID] = result.GetPrefabIDs();
                    }

                    m_Log.Debug($"{nameof(TreeControllerUISystem)}.{nameof(TryLoadCustomPrefabSet)} loaded repository for {prefabSetID}.");
                    return true;
                }
                catch (Exception ex)
                {
                    m_Log.Warn($"{nameof(TreeControllerUISystem)}.{nameof(TryLoadCustomPrefabSet)} Could not get default values for Set {prefabSetID}. Encountered exception {ex}");
                    return false;
                }
            }

            return false;
        }
    }
}
