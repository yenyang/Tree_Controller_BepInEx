// <copyright file="ReloadFoliageColorDataSystem.cs" company="Yenyangs Mods. MIT License">
// Copyright (c) Yenyangs Mods. MIT License. All rights reserved.
// </copyright>

// #define VERBOSE
namespace Tree_Controller.Systems
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Colossal.Entities;
    using Colossal.Logging;
    using Game;
    using Game.Areas;
    using Game.Common;
    using Game.Prefabs;
    using Game.Prefabs.Climate;
    using Game.Rendering;
    using Game.Simulation;
    using Game.Tools;
    using Tree_Controller.Settings;
    using Tree_Controller.Utils;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;
    using UnityEngine;

    /// <summary>
    /// Replaces colors of vanilla trees.
    /// </summary>
    public partial class ReloadFoliageColorDataSystem : GameSystemBase
    {
        private readonly Dictionary<TreeSeasonIdentifier, Game.Rendering.ColorSet> m_YenyangsColorSets = new ()
        {
            { new () { m_PrefabID = new ("StaticObjectPrefab", "AppleTree01"), m_Season = FoliageUtils.Season.Spring }, new () { m_Channel0 = new (0.409f, 0.509f, 0.344f, 1.000f), m_Channel1 = new (0.335f, 0.462f, 0.265f, 1.000f), m_Channel2 = new (0.945f, 0.941f, 0.957f, 1.000f) } },
            { new () { m_PrefabID = new ("StaticObjectPrefab", "AppleTree01"), m_Season = FoliageUtils.Season.Summer }, new () { m_Channel0 = new (0.409f, 0.509f, 0.344f, 1.000f), m_Channel1 = new (0.335f, 0.462f, 0.265f, 1.000f), m_Channel2 = new (0.625f, 0.141f, 0.098f, 1.000f) } },
            { new () { m_PrefabID = new ("StaticObjectPrefab", "AppleTree01"), m_Season = FoliageUtils.Season.Autumn }, new () { m_Channel0 = new (0.934f, 0.250f, 0.109f, 1.000f), m_Channel1 = new (0.785f, 0.313f, 0.137f, 1.000f), m_Channel2 = new (0.625f, 0.141f, 0.098f, 1.000f) } },
            { new () { m_PrefabID = new ("StaticObjectPrefab", "AppleTree01"), m_Season = FoliageUtils.Season.Winter }, new () { m_Channel0 = new (0.670f, 0.523f, 0.409f, 1.000f), m_Channel1 = new (0.642f, 0.574f, 0.428f, 1.000f), m_Channel2 = new (0.689f, 0.608f, 0.466f, 1.000f) } },
            { new () { m_PrefabID = new ("StaticObjectPrefab", "BirchTree01"), m_Season = FoliageUtils.Season.Autumn }, new () { m_Channel0 = new (0.981f, 0.773f, 0.270f, 1.000f), m_Channel1 = new (0.867f, 0.586f, 0.137f, 1.000f), m_Channel2 = new (0.957f, 0.664f, 0.199f, 1.000f) } },
            { new () { m_PrefabID = new ("StaticObjectPrefab", "EU_AlderTree01"), m_Season = FoliageUtils.Season.Autumn }, new () { m_Channel0 = new (0.409f, 0.509f, 0.344f, 1.000f), m_Channel1 = new (0.335f, 0.462f, 0.265f, 1.000f), m_Channel2 = new (0.373f, 0.500f, 0.324f, 1.000f) } },
            { new () { m_PrefabID = new ("StaticObjectPrefab", "EU_AlderTree01"), m_Season = FoliageUtils.Season.Winter }, new () { m_Channel0 = new (0.409f, 0.509f, 0.344f, 1.000f), m_Channel1 = new (0.335f, 0.462f, 0.265f, 1.000f), m_Channel2 = new (0.373f, 0.500f, 0.324f, 1.000f) } },
            { new () { m_PrefabID = new ("StaticObjectPrefab", "EU_ChestnutTree01"), m_Season = FoliageUtils.Season.Autumn }, new () { m_Channel0 = new (0.981f, 0.707f, 0.099f, 1.000f), m_Channel1 = new (0.961f, 0.531f, 0.031f, 1.000f), m_Channel2 = new (0.984f, 0.664f, 0.094f, 1.000f) } },
            { new () { m_PrefabID = new ("StaticObjectPrefab", "EU_ChestnutTree01"), m_Season = FoliageUtils.Season.Winter }, new () { m_Channel0 = new (0.606f, 0.219f, 0f, 1.000f), m_Channel1 = new (0.633f, 0.227f, 0.051f, 1.000f), m_Channel2 = new (0.379f, 0.082f, 0f, 1.000f) } },
            { new () { m_PrefabID = new ("StaticObjectPrefab", "EU_PoplarTree01"), m_Season = FoliageUtils.Season.Autumn }, new () { m_Channel0 = new (0.961f, 0.781f, 0.344f, 1.000f), m_Channel1 = new (0.793f, 0.613f, 0.141f, 1.000f), m_Channel2 = new (0.984f, 0.789f, 0.281f, 1.000f) } },
            { new () { m_PrefabID = new ("StaticObjectPrefab", "FlowerBushWild01"), m_Season = FoliageUtils.Season.Spring }, new () { m_Channel0 = new (0.310f, 0.463f, 0.310f, 1.000f), m_Channel1 = new (0.329f, 0.443f, 0.294f, 1.000f), m_Channel2 = new (0.32f, 0.45f, 0.3f, 1.000f) } },
            { new () { m_PrefabID = new ("StaticObjectPrefab", "FlowerBushWild02"), m_Season = FoliageUtils.Season.Summer }, new () { m_Channel0 = new (0.310f, 0.463f, 0.310f, 1.000f), m_Channel1 = new (0.329f, 0.443f, 0.294f, 1.000f), m_Channel2 = new (0.32f, 0.45f, 0.3f, 1.000f) } },
            { new () { m_PrefabID = new ("StaticObjectPrefab", "NA_HickoryTree01"), m_Season = FoliageUtils.Season.Autumn }, new () { m_Channel0 = new (0.965f, 0.805f, 0.066f, 1.000f), m_Channel1 = new (0.914f, 0.582f, 0.125f, 1.000f), m_Channel2 = new (0.863f, 0.504f, 0.242f, 1.000f) } },
            { new () { m_PrefabID = new ("StaticObjectPrefab", "NA_LindenTree01"), m_Season = FoliageUtils.Season.Autumn }, new () { m_Channel0 = new (0.930f, 0.600f, 0.008f, 1.000f), m_Channel1 = new (0.633f, 0.320f, 0.016f, 1.000f), m_Channel2 = new (0.852f, 0.297f, 0.004f, 1.000f) } },
            { new () { m_PrefabID = new ("StaticObjectPrefab", "NA_LondonPlaneTree01"), m_Season = FoliageUtils.Season.Autumn }, new () { m_Channel0 = new (0.922f, 0.535f, 0.106f, 1.000f), m_Channel1 = new (0.871f, 0.676f, 0.168f, 1.000f), m_Channel2 = new (0.543f, 0.188f, 0.070f, 1.000f) } },
            { new () { m_PrefabID = new ("StaticObjectPrefab", "NA_LondonPlaneTree01"), m_Season = FoliageUtils.Season.Winter }, new () { m_Channel0 = new (0.680f, 0.379f, 0.051f, 1.000f), m_Channel1 = new (0.605f, 0.340f, 0.063f, 1.000f), m_Channel2 = new (0.508f, 0.199f, 0.032f, 1.000f) } },
            { new () { m_PrefabID = new ("StaticObjectPrefab", "OakTree01"), m_Season = FoliageUtils.Season.Autumn }, new () { m_Channel0 = new (0.957f, 0.356f, 0.113f, 1.000f), m_Channel1 = new (0.957f, 0.266f, 0.125f, 1.000f), m_Channel2 = new (0.961f, 0.469f, 0.281f, 1.000f) } },
            { new () { m_PrefabID = new ("StaticObjectPrefab", "OakTree01"), m_Season = FoliageUtils.Season.Winter }, new () { m_Channel0 = new (0.934f, 0.25f, 0.109f, 1.000f), m_Channel1 = new (0.785f, 0.313f, 0.137f, 1.000f), m_Channel2 = new (0.902f, 0.148f, 0.059f, 1.000f) } },
        };

        private PrefabSystem m_PrefabSystem;
        private EntityQuery m_TreePrefabQuery;
        private JobHandle treePrefabJobHandle;
        private NativeList<Entity> m_TreePrefabEntities;
        private TreeControllerSettings.ColorVariationSetYYTC m_ColorVariationSet;
        private bool m_Run = true;
        private Dictionary<TreeSeasonIdentifier, Game.Rendering.ColorSet> m_VanillaColorSets;
        private ClimateSystem m_ClimateSystem;
        private FoliageUtils.Season m_Season = FoliageUtils.Season.Spring;
        private ILog m_Log;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReloadFoliageColorDataSystem"/> class.
        /// </summary>
        public ReloadFoliageColorDataSystem()
        {
        }

        /// <summary>
        /// Sets a value indicating whether to reload foliage color data.
        /// </summary>
        public bool Run { set => m_Run = value; }

        private Dictionary<TreeSeasonIdentifier, ColorSet> YenyangsColorSets => m_YenyangsColorSets;

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = TreeControllerMod.Instance.Logger;
            m_PrefabSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<PrefabSystem>();
            m_ClimateSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<ClimateSystem>();
            m_Log.Info($"{typeof(ReloadFoliageColorDataSystem)}.{nameof(OnCreate)}");
            m_ColorVariationSet = TreeControllerMod.Settings.ColorVariationSet;
            m_VanillaColorSets = new ();
            m_TreePrefabQuery = GetEntityQuery(new EntityQueryDesc[]
            {
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<TreeData>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<PlaceholderObjectElement>(),
                        ComponentType.ReadOnly<Evergreen>(),
                    },
                },
            });
            RequireForUpdate(m_TreePrefabQuery);
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            Entity currentClimate = m_ClimateSystem.currentClimate;
            if (currentClimate == Entity.Null)
            {
                return;
            }

            ClimatePrefab climatePrefab = m_PrefabSystem.GetPrefab<ClimatePrefab>(m_ClimateSystem.currentClimate);

            if (m_TreePrefabQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            FoliageUtils.Season lastSeason = m_Season;
            m_Season = FoliageUtils.GetSeasonFromSeasonID(climatePrefab.FindSeasonByTime(m_ClimateSystem.currentDate).Item1.m_NameID);
            if (lastSeason != m_Season)
            {
                m_Run = true;
            }

            if (!m_Run && TreeControllerMod.Settings.ColorVariationSet == m_ColorVariationSet)
            {
                return;
            }

            m_TreePrefabEntities = m_TreePrefabQuery.ToEntityListAsync(Allocator.Temp, out treePrefabJobHandle);
            treePrefabJobHandle.Complete();
            foreach (Entity e in m_TreePrefabEntities)
            {
                if (!EntityManager.TryGetBuffer<SubMesh>(e, isReadOnly: false, out DynamicBuffer<SubMesh> subMeshBuffer))
                {
                    continue;
                }

                for (int i = 0; i <= 3; i++)
                {
                    if (!EntityManager.TryGetBuffer<ColorVariation>(subMeshBuffer[i].m_SubMesh, isReadOnly: false, out DynamicBuffer<ColorVariation> colorVariationBuffer))
                    {
                        continue;
                    }

                    if (colorVariationBuffer.Length < 4)
                    {
                        EntityManager.AddComponent<Evergreen>(e);
                        continue;
                    }

                    for (int j = 0; j < 4; j++)
                    {
                        PrefabBase prefabBase = m_PrefabSystem.GetPrefab<PrefabBase>(e);
                        PrefabID prefabID = prefabBase.GetPrefabID();
#if VERBOSE
                        m_Log.Verbose($"{prefabID.GetName()} {(TreeState)(int)Math.Pow(2, i - 1)} {(FoliageUtils.Season)j} {colorVariationBuffer[j].m_ColorSet.m_Channel0} {colorVariationBuffer[j].m_ColorSet.m_Channel1} {colorVariationBuffer[j].m_ColorSet.m_Channel2}");
#endif
                        TreeSeasonIdentifier treeSeasonIdentifier = new ()
                        {
                            m_PrefabID = prefabID,
                            m_Season = (FoliageUtils.Season)j,
                        };

                        ColorVariation currentColorVariation = colorVariationBuffer[j];
                        if (!m_VanillaColorSets.ContainsKey(treeSeasonIdentifier))
                        {
                            m_VanillaColorSets.Add(treeSeasonIdentifier, currentColorVariation.m_ColorSet);
                        }

                        ExportDefaultColorSet(treeSeasonIdentifier, currentColorVariation);
                        bool setVanillaWinterToSpringColors = false;
                        if (TreeControllerMod.Settings.UseDeadModelDuringWinter && m_Season == FoliageUtils.Season.Spring && treeSeasonIdentifier.m_Season == FoliageUtils.Season.Winter)
                        {
                            treeSeasonIdentifier.m_Season = FoliageUtils.Season.Spring;
                            setVanillaWinterToSpringColors = true;
                        }

                        if (TreeControllerMod.Settings.ColorVariationSet == TreeControllerSettings.ColorVariationSetYYTC.Custom)
                        {
                            if (TryImportCustomColorSet(treeSeasonIdentifier, ref currentColorVariation.m_ColorSet))
                            {
                                colorVariationBuffer[j] = currentColorVariation;
                                m_Log.Debug($"{nameof(ReloadFoliageColorDataSystem)}.{nameof(OnUpdate)} Imported Colorset for {prefabID} in {treeSeasonIdentifier.m_Season}");
                            }
                        }
                        else if (TreeControllerMod.Settings.ColorVariationSet == TreeControllerSettings.ColorVariationSetYYTC.Yenyangs && YenyangsColorSets.ContainsKey(treeSeasonIdentifier))
                        {
                            currentColorVariation.m_ColorSet = YenyangsColorSets[treeSeasonIdentifier];
                            colorVariationBuffer[j] = currentColorVariation;
                            m_Log.Debug($"{nameof(ReloadFoliageColorDataSystem)}.{nameof(OnUpdate)} Changed Colorset for {prefabID} in {treeSeasonIdentifier.m_Season}");
                        }
                        else if (TreeControllerMod.Settings.ColorVariationSet == TreeControllerSettings.ColorVariationSetYYTC.Vanilla && m_VanillaColorSets.ContainsKey(treeSeasonIdentifier))
                        {
                            currentColorVariation.m_ColorSet = m_VanillaColorSets[treeSeasonIdentifier];
                            colorVariationBuffer[j] = currentColorVariation;
                            m_Log.Debug($"{nameof(ReloadFoliageColorDataSystem)}.{nameof(OnUpdate)} Reset Colorset for {prefabID} in {treeSeasonIdentifier.m_Season}");
                        }
                        else if (setVanillaWinterToSpringColors)
                        {
                            currentColorVariation.m_ColorSet = m_VanillaColorSets[treeSeasonIdentifier];
                            colorVariationBuffer[j] = currentColorVariation;
                            m_Log.Debug($"{nameof(ReloadFoliageColorDataSystem)}.{nameof(OnUpdate)} Reset Colorset for {prefabID} in {FoliageUtils.Season.Winter}");
                        }
                    }
                }

                m_TreePrefabEntities.Dispose();
                m_Run = false;
                m_ColorVariationSet = TreeControllerMod.Settings.ColorVariationSet;
            }
        }

        /// <summary>
        /// Exports CSVs with ColorVariationData and TreeSeasonIdentification.
        /// </summary>
        /// <param name="treeSeasonIdentifier">Information needed to identify tree PrefabID and season.</param>
        /// <param name="currentColorVariation">ColorVariation for tree Prefab and season.</param>
        private void ExportDefaultColorSet(TreeSeasonIdentifier treeSeasonIdentifier, ColorVariation currentColorVariation)
        {
            if (TreeControllerMod.ModInstallFolder != null)
            {
                string foliageColorDataFolderPath = Path.Combine(TreeControllerMod.ModInstallFolder, $"FoliageColorData/");
                System.IO.Directory.CreateDirectory(foliageColorDataFolderPath);
                string foliageColorDataFilePath = Path.Combine(foliageColorDataFolderPath, $"{treeSeasonIdentifier.m_PrefabID.GetName()}-{(int)treeSeasonIdentifier.m_Season}{treeSeasonIdentifier.m_Season}.csv");
#if VERBOSE
                m_Log.Verbose($"{typeof(ReloadFoliageColorDataSystem)}.{nameof(ExportDefaultColorSet)} foliageColorDataFilePath = {foliageColorDataFilePath}");
#endif
                if (!File.Exists(foliageColorDataFilePath))
                {
                    try
                    {
                        StreamWriter streamWriter = new (File.Create(foliageColorDataFilePath));
                        streamWriter.WriteLine($"Channel,R,G,B,A");
                        streamWriter.WriteLine($"Channel0,{currentColorVariation.m_ColorSet.m_Channel0.r},{currentColorVariation.m_ColorSet.m_Channel0.g},{currentColorVariation.m_ColorSet.m_Channel0.b},{currentColorVariation.m_ColorSet.m_Channel0.a}");
                        streamWriter.WriteLine($"Channel1,{currentColorVariation.m_ColorSet.m_Channel1.r},{currentColorVariation.m_ColorSet.m_Channel1.g},{currentColorVariation.m_ColorSet.m_Channel1.b},{currentColorVariation.m_ColorSet.m_Channel1.a}");
                        streamWriter.WriteLine($"Channel2,{currentColorVariation.m_ColorSet.m_Channel2.r},{currentColorVariation.m_ColorSet.m_Channel2.g},{currentColorVariation.m_ColorSet.m_Channel2.b},{currentColorVariation.m_ColorSet.m_Channel2.a}");
                        streamWriter.Close();
                    }
                    catch (Exception e)
                    {
                        m_Log.Info($"{typeof(ReloadFoliageColorDataSystem)}.{nameof(ExportDefaultColorSet)} Encountered Exception {e} while trying to export default color set for {treeSeasonIdentifier.m_PrefabID.GetName()} in {treeSeasonIdentifier.m_Season}.");
                    }
                }
            }
        }

        /// <summary>
        /// Try to import custom color set based on TreeSeasonIdentifier.
        /// </summary>
        /// <param name="treeSeasonIdentifier">The prefabID and season that identifies the colorVariation.</param>
        /// <param name="colorSet">The color set from the ColorVariation.</param>
        /// <returns>True is successfully imported colorset. False if unsuccessful.</returns>
        private bool TryImportCustomColorSet(TreeSeasonIdentifier treeSeasonIdentifier, ref ColorSet colorSet)
        {
            if (TreeControllerMod.ModInstallFolder != null)
            {
                string foliageColorDataFolderPath = Path.Combine(TreeControllerMod.ModInstallFolder, $"FoliageColorData/");
                System.IO.Directory.CreateDirectory(foliageColorDataFolderPath);
                string foliageColorDataFilePath = Path.Combine(foliageColorDataFolderPath, $"{treeSeasonIdentifier.m_PrefabID.GetName()}-{(int)treeSeasonIdentifier.m_Season}{treeSeasonIdentifier.m_Season}.csv");
                if (File.Exists(foliageColorDataFilePath))
                {
                    try
                    {
                        StreamReader streamReader = new (foliageColorDataFilePath);
                        string line = streamReader.ReadLine();
                        if ((line = streamReader.ReadLine()) != null)
                        {
                            CompileColorSet(line.Split(','), ref colorSet.m_Channel0);
                        }

                        if ((line = streamReader.ReadLine()) != null)
                        {
                            CompileColorSet(line.Split(','), ref colorSet.m_Channel1);
                        }

                        if ((line = streamReader.ReadLine()) != null)
                        {
                            CompileColorSet(line.Split(','), ref colorSet.m_Channel2);
                        }

                        streamReader.Close();
                        return true;
                    }
                    catch (Exception e)
                    {
                        m_Log.Info($"{typeof(ReloadFoliageColorDataSystem)}.{nameof(TryImportCustomColorSet)} Encountered Exception {e} while trying to import color set for {treeSeasonIdentifier.m_PrefabID.GetName()} in {treeSeasonIdentifier.m_Season}.");
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Takes a rawCSV line and returns a color.
        /// </summary>
        /// <param name="rawCSVLine">An array of string from the csv line. Elements 1-4 should be parsable float values between 0 and 1.</param>
        /// <param name="color">RGBA.</param>
        private void CompileColorSet(string[] rawCSVLine, ref UnityEngine.Color color)
        {
            if (rawCSVLine.Length >= 5)
            {
                TrySetColor(rawCSVLine[1], ref color.r);
                TrySetColor(rawCSVLine[2], ref color.g);
                TrySetColor(rawCSVLine[3], ref color.b);
                TrySetColor(rawCSVLine[4], ref color.a);
            }
        }

        /// <summary>
        /// Parses the rawCSV value and clamps it to be between 0f and 1f.
        /// </summary>
        /// <param name="rawCSV">Raw string from CSV.</param>
        /// <param name="rgba">A value between 0f and 1f to be used in a color.</param>
        private void TrySetColor(string rawCSV, ref float rgba)
        {
            rgba = Mathf.Clamp01(float.Parse(rawCSV));
        }

        private struct TreeSeasonIdentifier
        {
            public PrefabID m_PrefabID;
            public FoliageUtils.Season m_Season;
        }
    }
}
