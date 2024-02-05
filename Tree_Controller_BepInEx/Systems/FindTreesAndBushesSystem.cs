// <copyright file="FindTreesAndBushesSystem.cs" company="Yenyangs Mods. MIT License">
// Copyright (c) Yenyangs Mods. MIT License. All rights reserved.
// </copyright>

namespace Tree_Controller.Systems
{
    using Colossal.Entities;
    using Colossal.Logging;
    using Colossal.Serialization.Entities;
    using Game;
    using Game.Common;
    using Game.Objects;
    using Game.Prefabs;
    using Game.Rendering;
    using Game.Simulation;
    using Game.Tools;
    using System;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;

    /// <summary>
    /// Finds trees and bushes and adds Deciduous or Evergreen components.
    /// </summary>
    public partial class FindTreesAndBushesSystem : GameSystemBase
    {
        /// <summary>
        /// Relates to the update interval although the GetUpdateInterval isn't even using this.
        /// </summary>
        public const int UPDATES_PER_DAY = 32;
        private SimulationSystem m_SimulationSystem;
        private TimeSystem m_TimeSystem;
        private EntityQuery m_TreeQuery;
        private SafelyRemoveSystem m_SafelyRemoveSystem;
        private ILog m_Log;
        private EndFrameBarrier m_EndFrameBarrier;
        private TypeHandle __TypeHandle;
        private PrefabSystem m_PrefabSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="FindTreesAndBushesSystem"/> class.
        /// </summary>
        public FindTreesAndBushesSystem()
        {
        }

        /// <inheritdoc/>
        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return 512;
        }

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = TreeControllerMod.Instance.Logger;
            m_TimeSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<TimeSystem>();
            m_SimulationSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<SimulationSystem>();
            m_EndFrameBarrier = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<EndFrameBarrier>();
            m_SafelyRemoveSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<SafelyRemoveSystem>();
            m_PrefabSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<PrefabSystem>();
            m_Log.Info($"{nameof(FindTreesAndBushesSystem)} created!");

            m_TreeQuery = GetEntityQuery(new EntityQueryDesc[]
            {
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Game.Objects.Tree>(),
                        ComponentType.ReadOnly<Game.Prefabs.PrefabRef>(),
                        ComponentType.ReadOnly<UpdateFrame>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadOnly<Evergreen>(),
                        ComponentType.ReadOnly<Overridden>(),
                        ComponentType.ReadOnly<Lumber>(),
                        ComponentType.ReadOnly<DeciduousData>(),
                    },
                },
            });
            RequireForUpdate(m_TreeQuery);
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            if (!m_TreeQuery.IsEmptyIgnoreFilter && TreeControllerMod.Settings.UseDeadModelDuringWinter)
            {
                __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle.Update(ref CheckedStateRef);
                __TypeHandle.__Game_Objects_Tree_RO_ComponentTypeHandle.Update(ref CheckedStateRef);
                __TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref CheckedStateRef);
                __TypeHandle.__Evergreen_ComponentLookup.Update(ref CheckedStateRef);

                uint updateFrame = SimulationUtils.GetUpdateFrame(m_SimulationSystem.frameIndex, 32, 16);
                m_TreeQuery.ResetFilter();
                m_TreeQuery.SetSharedComponentFilter(new UpdateFrame(updateFrame));
                FindTreePrefabRefsJob findTreePrefabRefJob = new()
                {
                    m_PrefabRefType = __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle,
                    m_EntityType = __TypeHandle.__Unity_Entities_Entity_TypeHandle,
                    m_TreeType = __TypeHandle.__Game_Objects_Tree_RO_ComponentTypeHandle,
                    buffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                    m_EvergreenData = __TypeHandle.__Evergreen_ComponentLookup,
                };
                JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(findTreePrefabRefJob, m_TreeQuery, Dependency);
                m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
                Dependency = jobHandle;
                if (m_SafelyRemoveSystem.Enabled)
                {
                    m_SafelyRemoveSystem.Enabled = false;
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            PrefabID vegetationPrefabID = new PrefabID("UIAssetCategoryPrefab", "Vegetation");
            if (!m_PrefabSystem.TryGetPrefab(vegetationPrefabID, out PrefabBase vegetationPrefab))
            {
                m_Log.Error(new Exception("Tree controller cound not find the vegetation tab prefab"));
                return;
            }

            if (!m_PrefabSystem.TryGetEntity(vegetationPrefab, out Entity vegetationEntity))
            {
                m_Log.Error(new Exception("Tree controller cound not find the vegetation tab entity"));
                return;
            }

            if (!EntityManager.TryGetBuffer(vegetationEntity, true, out DynamicBuffer<UIGroupElement> buffer))
            {
                m_Log.Error(new Exception("Tree controller cound not find the vegetation tab group element buffer."));
                return;
            }

            foreach (UIGroupElement element in buffer)
            {
                EntityManager.AddComponent<Vegetation>(element.m_Prefab);
            }

            base.OnGameLoadingComplete(purpose, mode);
        }

        /// <inheritdoc/>
        protected override void OnCreateForCompiler()
        {
            base.OnCreateForCompiler();
            __TypeHandle.__AssignHandles(ref CheckedStateRef);
        }

        private struct TypeHandle
        {
            [ReadOnly]
            public ComponentTypeHandle<Game.Prefabs.PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;
            [ReadOnly]
            public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<Game.Objects.Tree> __Game_Objects_Tree_RO_ComponentTypeHandle;
            [ReadOnly]
            public ComponentLookup<Evergreen> __Evergreen_ComponentLookup;

            public void __AssignHandles(ref SystemState state)
            {

                __Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
                __Game_Objects_Tree_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.Tree>();
                __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Prefabs.PrefabRef>();
                __Evergreen_ComponentLookup = state.GetComponentLookup<Evergreen>();
            }
        }

        private struct FindTreePrefabRefsJob : IJobChunk
        {
            [ReadOnly]
            public EntityTypeHandle m_EntityType;
            [ReadOnly]
            public ComponentTypeHandle<Game.Prefabs.PrefabRef> m_PrefabRefType;
            public EntityCommandBuffer.ParallelWriter buffer;
            [ReadOnly]
            public ComponentTypeHandle<Game.Objects.Tree> m_TreeType;
            [ReadOnly]
            public ComponentLookup<Evergreen> m_EvergreenData;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entityNativeArray = chunk.GetNativeArray(m_EntityType);
                NativeArray<Game.Prefabs.PrefabRef> prefabRefNativeArray = chunk.GetNativeArray(ref m_PrefabRefType);
                NativeArray<Game.Objects.Tree> treeNativeArray = chunk.GetNativeArray(ref m_TreeType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    Entity currentEntity = entityNativeArray[i];
                    Game.Prefabs.PrefabRef currentPrefabRef = prefabRefNativeArray[i];
                    Entity prefabEntity = currentPrefabRef.m_Prefab;
                    Game.Objects.Tree currentTreeData = treeNativeArray[i];
                    if (!m_EvergreenData.HasComponent(prefabEntity)) // Is Deciduous?
                    {
                        DeciduousData currentDeciduousTreeData = default;
                        currentDeciduousTreeData.m_PreviousTreeState = currentTreeData.m_State;
                        if (currentTreeData.m_State == TreeState.Dead)
                        {
                            currentDeciduousTreeData.m_TechnicallyDead = true;
                        }
                        else
                        {
                            currentDeciduousTreeData.m_TechnicallyDead = false;
                        }

                        buffer.AddComponent(unfilteredChunkIndex, currentEntity, currentDeciduousTreeData);
                    }
                    else
                    {
                        buffer.AddComponent(unfilteredChunkIndex, currentEntity, default(Evergreen));
                    }
                }
            }
        }
    }
}
