// <copyright file="TreeControllerMod.cs" company="Yenyangs Mods. MIT License">
// Copyright (c) Yenyangs Mods. MIT License. All rights reserved.
// </copyright>

// #define VERBOSE
namespace Tree_Controller
{
    using System;
    using System.IO;
    using System.Linq;
    using Colossal.IO.AssetDatabase;
    using Colossal.Localization;
    using Colossal.Logging;
    using Game;
    using Game.Modding;
    using Game.SceneFlow;
    using Tree_Controller.Settings;
    using Tree_Controller.Systems;
    using Tree_Controller.Tools;

    /// <summary>
    /// Mod entry point.
    /// </summary>
    public class TreeControllerMod : IMod
    {
        /// <summary>
        /// Gets the install folder for the mod.
        /// </summary>
        private static string m_modInstallFolder;

        /// <summary>
        /// Gets the static reference to the mod instance.
        /// </summary>
        public static TreeControllerMod Instance
        {
            get;
            private set;
        }

        /// <summary>
        ///  Gets or sets the static version of the Mod Settings.
        /// </summary>
        public static TreeControllerSettings Settings { get; set; }

        /// <summary>
        /// Gets the Install Folder for the mod as a string.
        /// </summary>
        public static string ModInstallFolder
        {
            get
            {
                if (m_modInstallFolder is null)
                {
                    m_modInstallFolder = Path.GetDirectoryName(typeof(TreeControllerPlugin).Assembly.Location);
                }

                return m_modInstallFolder;
            }
        }

        /// <summary>
        /// Gets ILog for mod.
        /// </summary>
        internal ILog Logger { get; private set; }

        /// <summary>
        /// Adds custom components to database. Sets up instance, and logger.
        /// </summary>
        public void OnLoad()
        {
            Instance = this;
            Logger = LogManager.GetLogger("Mods_Yenyang_Tree_Controller", false);
#if VERBOSE
            Logger.effectivenessLevel = Level.Verbose;
            Logger.Verbose($"Loaded component count: {TypeManager.GetTypeCount()}");
#endif
            Logger.Info($"[{nameof(TreeControllerMod)}] {nameof(OnLoad)}");
#if VERBOSE
            Logger.Debug(Settings.GetEnumValueLocaleID(TreeControllerSettings.ColorVariationSetYYTC.Yenyangs));
            Logger.Verbose(Settings.GetSettingsLocaleID());
            Logger.Verbose(Settings.GetOptionLabelLocaleID(nameof(TreeControllerSettings.ResetModSettings)));
            Logger.Verbose(Settings.GetOptionDescLocaleID(nameof(TreeControllerSettings.ResetModSettings)));
            Logger.Verbose(Settings.GetOptionWarningLocaleID(nameof(TreeControllerSettings.ResetModSettings)));
#endif
        }

        /// <inheritdoc/>
        public void OnCreateWorld(UpdateSystem updateSystem)
        {
            Logger.Info($"[{nameof(TreeControllerMod)}] {nameof(OnCreateWorld)}");
            Logger.effectivenessLevel = Level.Debug;  // Remember to change this before release.
            Settings = new (this);
            Settings.RegisterInOptionsUI();
            AssetDatabase.global.LoadSettings(nameof(TreeControllerMod), Settings, new TreeControllerSettings(this));
            Settings.Contra = false;
            Logger.Info($"[{nameof(TreeControllerMod)}] {nameof(OnCreateWorld)} finished loading settings.");
            LoadLocales();
            Logger.Info($"[{nameof(TreeControllerMod)}] {nameof(OnCreateWorld)} finished i18n.");

            updateSystem.UpdateAt<TreeControllerTool>(SystemUpdatePhase.ToolUpdate);
            updateSystem.UpdateBefore<TreeObjectDefinitionSystem>(SystemUpdatePhase.Modification1);
            updateSystem.UpdateAt<TreeControllerUISystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<TreeControllerTooltipSystem>(SystemUpdatePhase.UITooltip);
            updateSystem.UpdateAt<ClearTreeControllerTool>(SystemUpdatePhase.ClearTool);
            updateSystem.UpdateBefore<FindTreesAndBushesSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateBefore<DeciduousSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateBefore<ReloadFoliageColorDataSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAfter<ReloadFoliageColorDataSystem>(SystemUpdatePhase.PrefabUpdate);
            updateSystem.UpdateBefore<ModifyTreeGrowthSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateBefore<SafelyRemoveSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateBefore<LumberSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<DetectAreaChangeSystem>(SystemUpdatePhase.ModificationEnd);

            Logger.Info($"[{nameof(TreeControllerMod)}] {nameof(OnCreateWorld)} finished systems");
        }

        /// <inheritdoc/>
        public void OnDispose()
        {
            Logger.Info("Disposing..");
        }

        /// <summary>
        /// Loads csv files for localization.
        /// </summary>
        private void LoadLocales()
        {
            var file = Path.Combine(ModInstallFolder, $"i18n.csv");
            if (File.Exists(file))
            {
                var fileLines = File.ReadAllLines(file).Select(x => x.Split('\t'));
                var enColumn = Array.IndexOf(fileLines.First(), "en-US");
                var enMemoryFile = new MemorySource(fileLines.Skip(1).ToDictionary(x => x[0], x => x.ElementAtOrDefault(enColumn)));
                foreach (var lang in GameManager.instance.localizationManager.GetSupportedLocales())
                {
                    GameManager.instance.localizationManager.AddSource(lang, enMemoryFile);
                    if (lang != "en-US")
                    {
                        var valueColumn = Array.IndexOf(fileLines.First(), lang);
                        if (valueColumn > 0)
                        {
                            var i18nFile = new MemorySource(fileLines.Skip(1).ToDictionary(x => x[0], x => x.ElementAtOrDefault(valueColumn)));
                            GameManager.instance.localizationManager.AddSource(lang, i18nFile);
                        }
                    }
                }
            }
            else
            {
                Logger.Info($"[{nameof(TreeControllerMod)}] {nameof(LoadLocales)} Couldn't find localization files. Used Mod Generated defaults.");
                foreach (var lang in GameManager.instance.localizationManager.GetSupportedLocales())
                {
                     GameManager.instance.localizationManager.AddSource(lang, new LocaleEN(Settings));
                }
            }
        }
    }
}
