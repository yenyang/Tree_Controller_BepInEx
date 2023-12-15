// <copyright file="ModifyTreeGrowthSystem.cs" company="Yenyangs Mods. MIT License">
// Copyright (c) Yenyangs Mods. MIT License. All rights reserved.
// </copyright>

namespace Tree_Controller.Systems
{
    using Colossal.Logging;
    using Game;
    using Game.Common;
    using Game.Prefabs;
    using Game.Prefabs.Climate;
    using Game.Simulation;
    using Game.Tools;
    using Tree_Controller.Utils;
    using Unity.Entities;

    /// <summary>
    /// System overrides query for TreeGrowthSystem. Not compatible with other mods that alter this query.
    /// </summary>
    public partial class ModifyTreeGrowthSystem : GameSystemBase
    {
        private TreeGrowthSystem m_TreeGrowthSystem;
        private EntityQuery m_DefaultTreeGrowthQuery;
        private EntityQuery m_DisableTreeGrowthQuery;
        private EntityQuery m_WinterDeciduousTreeGrowthQuery;
        private bool m_Run = true;
        private bool m_TreeGrowthDisabled = false;
        private ClimateSystem m_ClimateSystem;
        private PrefabSystem m_PrefabSystem;
        private FoliageUtils.Season m_Season = FoliageUtils.Season.Spring;
        private ILog m_Log;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModifyTreeGrowthSystem"/> class.
        /// </summary>
        public ModifyTreeGrowthSystem()
        {
        }

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = TreeControllerMod.Instance.Logger;
            m_TreeGrowthSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<TreeGrowthSystem>();
            m_ClimateSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<ClimateSystem>();
            m_PrefabSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<PrefabSystem>();
            m_Log.Info($"[{nameof(ModifyTreeGrowthSystem)}] {nameof(OnCreate)}");
            m_DefaultTreeGrowthQuery = GetEntityQuery(new EntityQueryDesc[]
            {
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadWrite<Game.Objects.Tree>(),
                        ComponentType.ReadOnly<UpdateFrame>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadOnly<Overridden>(),
                    },
                },
            });
            m_DisableTreeGrowthQuery = GetEntityQuery(new EntityQueryDesc[]
            {
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadWrite<Game.Objects.Tree>(),
                        ComponentType.ReadOnly<Lumber>(),
                        ComponentType.ReadOnly<UpdateFrame>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadOnly<Overridden>(),
                    },
                },
            });
            m_WinterDeciduousTreeGrowthQuery = GetEntityQuery(new EntityQueryDesc[]
            {
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadWrite<Game.Objects.Tree>(),
                        ComponentType.ReadOnly<UpdateFrame>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadOnly<Overridden>(),
                        ComponentType.ReadOnly<DeciduousData>(),
                    },
                },
            });


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

            FoliageUtils.Season lastSeason = m_Season;
            m_Season = FoliageUtils.GetSeasonFromSeasonID(climatePrefab.FindSeasonByTime(m_ClimateSystem.currentDate).Item1.m_NameID);
            if (lastSeason != m_Season)
            {
                m_Run = true;
            }

            if (!m_Run && TreeControllerMod.Settings.DisableTreeGrowth == m_TreeGrowthDisabled)
            {
                return;
            }

            if (TreeControllerMod.Settings.DisableTreeGrowth)
            {
                m_TreeGrowthSystem.SetMemberValue("m_TreeQuery", m_DisableTreeGrowthQuery);
                m_Log.Info($"[{nameof(ModifyTreeGrowthSystem)}] {nameof(OnUpdate)} DisableTreeGrowthQuery Activated.");
                m_TreeGrowthSystem.RequireForUpdate(m_DisableTreeGrowthQuery);
            }
            else if (m_Season != FoliageUtils.Season.Winter)
            {
                m_TreeGrowthSystem.SetMemberValue("m_TreeQuery", m_DefaultTreeGrowthQuery);
                m_Log.Info($"[{nameof(ModifyTreeGrowthSystem)}] {nameof(OnUpdate)} m_DefaultTreeGrowthQuery Activated.");
                m_TreeGrowthSystem.RequireForUpdate(m_DefaultTreeGrowthQuery);
            } 
            else
            {
                m_TreeGrowthSystem.SetMemberValue("m_TreeQuery", m_WinterDeciduousTreeGrowthQuery);
                m_Log.Info($"[{nameof(ModifyTreeGrowthSystem)}] {nameof(OnUpdate)} m_WinterDeciduousTreeGrowthQuery Activated.");
                m_TreeGrowthSystem.RequireForUpdate(m_WinterDeciduousTreeGrowthQuery);
            }

            m_TreeGrowthDisabled = TreeControllerMod.Settings.DisableTreeGrowth;
            m_Run = false;

        }
    }
}