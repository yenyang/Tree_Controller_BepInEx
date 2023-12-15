// <copyright file="SafelyRemoveSystem.cs" company="Yenyangs Mods. MIT License">
// Copyright (c) Yenyangs Mods. MIT License. All rights reserved.
// </copyright>

namespace Tree_Controller.Systems
{
    using Colossal.Logging;
    using Game;
    using Game.Areas;
    using Game.Common;
    using Game.Objects;
    using Game.Simulation;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;

    /// <summary>
    /// A system for resetting model states and removing custom components.
    /// </summary>
    public partial class SafelyRemoveSystem : GameSystemBase
    {
        private EndFrameBarrier m_EndFrameBarrier;
        private TimeSystem m_TimeSystem;
        private EntityQuery m_DeciduousTreeQuery;
        private ILog m_Log;
        private TypeHandle __TypeHandle;

        /// <summary>
        /// Initializes a new instance of the <see cref="SafelyRemoveSystem"/> class.
        /// </summary>
        public SafelyRemoveSystem()
        {
        }

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = TreeControllerMod.Instance.Logger;
            m_EndFrameBarrier = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<EndFrameBarrier>();
            m_TimeSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<TimeSystem>();
            Enabled = false;
            m_Log.Info($"{nameof(SafelyRemoveSystem)}.{nameof(OnCreate)}");

            m_DeciduousTreeQuery = GetEntityQuery(new EntityQueryDesc[]
           {
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadWrite<DeciduousData>(),
                        ComponentType.ReadWrite<Game.Objects.Tree>(),
                    },
                },
           });
            RequireForUpdate(m_DeciduousTreeQuery);
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            if (m_DeciduousTreeQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            __TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref CheckedStateRef);
            __TypeHandle.__Deciduousdata_ComponentTypeHandle.Update(ref CheckedStateRef);
            __TypeHandle.__Game_Objects_Tree_ComponentTypeHandle.Update(ref CheckedStateRef);

            TreeSeasonChangeJob treeSeasonChangeJob = new ()
            {
                m_DeciduousTreeDataType = __TypeHandle.__Deciduousdata_ComponentTypeHandle,
                m_EntityType = __TypeHandle.__Unity_Entities_Entity_TypeHandle,
                m_TreeType = __TypeHandle.__Game_Objects_Tree_ComponentTypeHandle,
                buffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
            };

            JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(treeSeasonChangeJob, m_DeciduousTreeQuery, Dependency);
            m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
            Dependency = jobHandle;
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
            public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;
            public ComponentTypeHandle<Tree> __Game_Objects_Tree_ComponentTypeHandle;
            public ComponentTypeHandle<DeciduousData> __Deciduousdata_ComponentTypeHandle;

            public void __AssignHandles(ref SystemState state)
            {
                __Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
                __Game_Objects_Tree_ComponentTypeHandle = state.GetComponentTypeHandle<Tree>();
                __Deciduousdata_ComponentTypeHandle = state.GetComponentTypeHandle<DeciduousData>();
            }
        }

        private struct TreeSeasonChangeJob : IJobChunk
        {
            [ReadOnly]
            public EntityTypeHandle m_EntityType;
            public ComponentTypeHandle<Game.Objects.Tree> m_TreeType;
            public ComponentTypeHandle<DeciduousData> m_DeciduousTreeDataType;
            public EntityCommandBuffer.ParallelWriter buffer;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entityNativeArray = chunk.GetNativeArray(m_EntityType);
                NativeArray<Game.Objects.Tree> treeNativeArray = chunk.GetNativeArray(ref m_TreeType);
                NativeArray<DeciduousData> deciduousTreeNativeArray = chunk.GetNativeArray(ref m_DeciduousTreeDataType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    Entity currentEntity = entityNativeArray[i];
                    Game.Objects.Tree currentTreeData = treeNativeArray[i];
                    DeciduousData currentDeciduousTreeData = deciduousTreeNativeArray[i];
                    buffer.AddComponent<BatchesUpdated>(unfilteredChunkIndex, currentEntity, default);
                    if (currentTreeData.m_State == TreeState.Dead && currentDeciduousTreeData.m_TechnicallyDead == false)
                    {
                        currentTreeData.m_State = currentDeciduousTreeData.m_PreviousTreeState;
                        buffer.SetComponent(unfilteredChunkIndex, currentEntity, currentTreeData);
                    }

                    buffer.RemoveComponent<DeciduousData>(unfilteredChunkIndex, currentEntity);
                }
            }
        }
    }
}
