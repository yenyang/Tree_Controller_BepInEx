// <copyright file="TreeControllerSettings.cs" company="Yenyangs Mods. MIT License">
// Copyright (c) Yenyangs Mods. MIT License. All rights reserved.
// </copyright>

namespace Tree_Controller.Settings
{
    using Colossal.IO.AssetDatabase;
    using Game.Modding;
    using Game.Settings;
    using Tree_Controller.Systems;
    using Unity.Entities;

    /// <summary>
    /// The mod settings for the Anarchy Mod.
    /// </summary>
    [FileLocation("Mods_Yenyang_Tree_Controller")]
    public class TreeControllerSettings : ModSetting
    {
        private ReloadFoliageColorDataSystem m_ReloadFoliageColorDataSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="TreeControllerSettings"/> class.
        /// </summary>
        /// <param name="mod">TreeControllerMod.</param>
        public TreeControllerSettings(IMod mod)
            : base(mod)
        {
            SetDefaults();
        }

        /// <summary>
        /// An enum for choosing a set of ColorVariations for seasonal Tree Foliage.
        /// </summary>
        public enum ColorVariationSetYYTC
        {
            /// <summary>
            /// Use the game's vanilla seasonal tree foliage.
            /// </summary>
            Vanilla,

            /// <summary>
            /// Use Yenyangs researched seasonal tree foliage.
            /// </summary>
            Yenyangs,

            /// <summary>
            /// Load custom seasonal tree foliage colors.
            /// </summary>
            Custom,
        }

        /// <summary>
        /// An enum for choosing the age selection
        /// </summary>
        public enum AgeSelectionOptions
        {
            /// <summary>
            /// random selection with equal weight.
            /// </summary>
            RandomEqualWeight,

            /// <summary>
            /// Uses vanilla weight for randomly selecting trees.
            /// </summary>
            RandomWeighted,
        }

        /// <summary>
        /// Gets or sets a value indicating whether Deciduous trees use Dead model during winter.
        /// </summary>
        public bool UseDeadModelDuringWinter { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether tree growth is disabled globally.
        /// </summary>
        public bool DisableTreeGrowth { get; set; }

        /// <summary>
        /// Gets or sets a enum that defines the selection for Age Selection.
        /// </summary>
        public AgeSelectionOptions AgeSelectionTechnique { get; set; }

        /// <summary>
        /// Gets or sets a enum that defines the type of Seasonal foliage color set preference.
        /// </summary>
        public ColorVariationSetYYTC ColorVariationSet { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use random rotation while plopping trees.
        /// </summary>
        [SettingsUIHidden]
        public bool RandomRotation { get; set; }

        /// <summary>
        /// Sets a value indicating whether . A button for triggering csv reload.
        /// </summary>
        [SettingsUIButton]
        [SettingsUIConfirmation]
        public bool ReloadCSVsButton
        {
            set
            {
                m_ReloadFoliageColorDataSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<ReloadFoliageColorDataSystem>();
                m_ReloadFoliageColorDataSystem.Run = true;
            }
        }

        /// <summary>
        /// Sets a value indicating whether the mod needs to safely remove components and reset models.
        /// </summary>
        [SettingsUIButton]
        [SettingsUIConfirmation]
        public bool SafelyRemoveButton
        {
            set
            {
                UseDeadModelDuringWinter = false;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether: Used to force saving of Modsettings if settings would result in empty Json.
        /// </summary>
        [SettingsUIHidden]
        public bool Contra { get; set; }

        /// <summary>
        /// Sets a value indicating whether: a button for Resetting the settings for the Mod.
        /// </summary>
        [SettingsUIButton]
        [SettingsUIConfirmation]
        public bool ResetModSettings
        {
            set
            {
                SetDefaults();
                Contra = false;
                ApplyAndSave();
            }
        }

        /// <inheritdoc/>
        public override void SetDefaults()
        {
            Contra = true;
            DisableTreeGrowth = false;
            ColorVariationSet = ColorVariationSetYYTC.Vanilla;
            UseDeadModelDuringWinter = false;
            AgeSelectionTechnique = AgeSelectionOptions.RandomWeighted;
        }
    }
}
