// <copyright file="LocaleEN.cs" company="Yenyangs Mods. MIT License">
// Copyright (c) Yenyangs Mods. MIT License. All rights reserved.
// </copyright>

namespace Tree_Controller.Settings
{
    using System.Collections.Generic;
    using Colossal;
    using Colossal.IO.AssetDatabase.Internal;
    using UnityEngine;

    /// <summary>
    /// Localization for <see cref="TreeControllerSettings"/> in English.
    /// </summary>
    public class LocaleEN : IDictionarySource
    {
        private readonly TreeControllerSettings m_Setting;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocaleEN"/> class.
        /// </summary>
        /// <param name="setting">Settings class.</param>
        public LocaleEN(TreeControllerSettings setting)
        {
            m_Setting = setting;
        }

        /// <inheritdoc/>
        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                { m_Setting.GetSettingsLocaleID(), "Tree Controller" },
                { m_Setting.GetOptionLabelLocaleID(nameof(TreeControllerSettings.UseDeadModelDuringWinter)), "Deciduous trees use Dead Model during Winter" },
                { m_Setting.GetOptionDescLocaleID(nameof(TreeControllerSettings.UseDeadModelDuringWinter)), "Will temporarily make all non-lumber industry deciduous trees use the dead model and pause growth during winter." },
                { m_Setting.GetOptionLabelLocaleID(nameof(TreeControllerSettings.DisableTreeGrowth)), "Disable Tree Growth" },
                { m_Setting.GetOptionDescLocaleID(nameof(TreeControllerSettings.DisableTreeGrowth)), "Disable tree growth for the entire map except for lumber industry." },
                { m_Setting.GetOptionLabelLocaleID(nameof(TreeControllerSettings.ColorVariationSet)), "Color Variation Set" },
                { m_Setting.GetOptionDescLocaleID(nameof(TreeControllerSettings.ColorVariationSet)), "Sets of seasonal colors for Trees and Wild bushes. Vanilla is the base game. Yenyang's is my curated colors. Custom uses CSV files in the mod folder." },
                { m_Setting.GetOptionLabelLocaleID(nameof(TreeControllerSettings.ReloadCSVsButton)), "Reload CSVs" },
                { m_Setting.GetOptionDescLocaleID(nameof(TreeControllerSettings.ReloadCSVsButton)), "After confirmation this will reload CSV files." },
                { m_Setting.GetOptionWarningLocaleID(nameof(TreeControllerSettings.ReloadCSVsButton)), "Reload CSV files?" },
                { m_Setting.GetOptionLabelLocaleID(nameof(TreeControllerSettings.SafelyRemoveButton)), "Safely Remove" },
                { m_Setting.GetOptionDescLocaleID(nameof(TreeControllerSettings.SafelyRemoveButton)), "Removes Tree Controller mod components and resets tree and bush model states. Only necessary during Winter and very end of Autumn. Must use reset button to undo setting change." },
                { m_Setting.GetOptionWarningLocaleID(nameof(TreeControllerSettings.SafelyRemoveButton)), "Remove Tree Controller mod components and reset tree and bush model states?" },
                { m_Setting.GetOptionLabelLocaleID(nameof(TreeControllerSettings.ResetModSettings)), "Reset Tree Controller Settings" },
                { m_Setting.GetOptionDescLocaleID(nameof(TreeControllerSettings.ResetModSettings)), "After confirmation this will reset Tree Controller Settings." },
                { m_Setting.GetOptionWarningLocaleID(nameof(TreeControllerSettings.ResetModSettings)), "Reset Tree Controller Settings?" },
                { m_Setting.GetEnumValueLocaleID(TreeControllerSettings.ColorVariationSetYYTC.Yenyangs), "Yenyang's" },
                { m_Setting.GetEnumValueLocaleID(TreeControllerSettings.ColorVariationSetYYTC.Vanilla), "Vanilla" },
                { m_Setting.GetEnumValueLocaleID(TreeControllerSettings.ColorVariationSetYYTC.Custom), "Custom" },
                { m_Setting.GetEnumValueLocaleID(TreeControllerSettings.AgeSelectionOptions.RandomEqualWeight), "Random: Equal Weight" },
                { m_Setting.GetEnumValueLocaleID(TreeControllerSettings.AgeSelectionOptions.RandomWeighted), "Random: Weighted" },
                { m_Setting.GetOptionLabelLocaleID(nameof(TreeControllerSettings.AgeSelectionTechnique)), "Age Selection Technique" },
                { m_Setting.GetOptionDescLocaleID(nameof(TreeControllerSettings.AgeSelectionTechnique)), "When multiple Tree Ages are selected, one will be selected using this option. Random: Equal Weight is just a random selection. Random: Weighted randomly selected using game's editor weights. Sequential: does selected ages in order." },
                { "Options.TOOLTIPYYTC[WholeMapApply]", "Right Click to Apply." },
            };

        }

        /// <inheritdoc/>
        public void Unload()
        {
        }
    }
}
