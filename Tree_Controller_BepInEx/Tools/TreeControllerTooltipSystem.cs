// <copyright file="TreeControllerTooltipSystem.cs" company="Yenyangs Mods. MIT License">
// Copyright (c) Yenyangs Mods. MIT License. All rights reserved.
// </copyright>

namespace Tree_Controller.Tools
{
    using System.Collections.Generic;
    using Game.Tools;
    using Game.UI.Localization;
    using Game.UI.Tooltip;
    using Unity.Entities;

    /// <summary>
    /// Tooltip system for Tree Controller Tool.
    /// </summary>
    public partial class TreeControllerTooltipSystem : TooltipSystemBase
    {
        /// <summary>
        /// A dictionary of ToolMode Tooltips.
        /// </summary>
        private readonly Dictionary<TreeControllerTool.TCSelectionMode, StringTooltip> m_ToolModeToolTipsDictionary = new ()
        {
             { TreeControllerTool.TCSelectionMode.WholeMap, new StringTooltip() { path = "Options.TOOLTIPYYTC[WholeMapApply]", value = LocalizedString.IdWithFallback("Options.TOOLTIPYYTC[WholeMapApply]", "Right Click to Apply.") } },
        };

        private ToolSystem m_ToolSystem;
        private TreeControllerTool m_TCTool;
        private StringTooltip m_ToolModeTooltip;

        /// <summary>
        /// Initializes a new instance of the <see cref="TreeControllerTooltipSystem"/> class.
        /// </summary>
        public TreeControllerTooltipSystem()
        {
        }

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_ToolSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<ToolSystem>();
            m_TCTool = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<TreeControllerTool>();
            m_ToolModeTooltip = new StringTooltip();
            if (m_ToolModeToolTipsDictionary.ContainsKey(m_TCTool.SelectionMode))
            {
                m_ToolModeTooltip = m_ToolModeToolTipsDictionary[m_TCTool.SelectionMode];
            }
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            if (m_ToolSystem.activeTool != m_TCTool)
            {
                return;
            }

            if (m_ToolModeToolTipsDictionary.ContainsKey(m_TCTool.SelectionMode))
            {
                m_ToolModeTooltip = m_ToolModeToolTipsDictionary[m_TCTool.SelectionMode];
                AddMouseTooltip(m_ToolModeTooltip);
            }
        }

        /// <inheritdoc/>
        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}
