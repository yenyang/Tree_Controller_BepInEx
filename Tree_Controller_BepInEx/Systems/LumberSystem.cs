// <copyright file="LumberSystem.cs" company="Yenyangs Mods. MIT License">
// Copyright (c) Yenyangs Mods. MIT License. All rights reserved.
// </copyright>

namespace Tree_Controller.Systems
{
    using Colossal.Logging;
    using Game;
    using Game.Areas;
    using Game.Common;
    using Game.Simulation;
    using Game.Tools;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;

    /// <summary>
    /// Adds/Removes lumber to/from WoodResource trees.
    /// </summary>
    public partial class LumberSystem : GameSystemBase
    {
        private SimulationSystem m_SimulationSystem;
        private TimeSystem m_TimeSystem;
        private EntityQuery m_WoodResourceQuery;
        private EntityQuery m_LumberQuery;
        private SafelyRemoveSystem m_SafelyRemoveSystem;
        private ILog m_Log;
        private EndFrameBarrier m_EndFrameBarrier;
        private TypeHandle __TypeHandle;

        /// <summary>
        /// Initializes a new instance of the <see cref="LumberSystem"/> class.
        /// </summary>
        public LumberSystem()
        {
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
            m_Log.Info($"{nameof(LumberSystem)} created!");

            m_WoodResourceQuery = GetEntityQuery(new EntityQueryDesc[]
            {
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadWrite<WoodResource>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<Temp>(),
                    },
                },
            });

            m_LumberQuery = GetEntityQuery(new EntityQueryDesc[]
            {
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadWrite<Lumber>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<Temp>(),
                    },
                },
            });

            RequireForUpdate(m_WoodResourceQuery);
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            __TypeHandle.__WoodResource_RO_BufferTypeHandle.Update(ref CheckedStateRef);
            __TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref CheckedStateRef);

            JobHandle lumberJobHandle = new ();
            if (!m_LumberQuery.IsEmptyIgnoreFilter)
            {
                RemoveLumberJob removeLumberJob = new ()
                {
                    buffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                    m_EntityType = __TypeHandle.__Unity_Entities_Entity_TypeHandle,
                };
                lumberJobHandle = JobChunkExtensions.ScheduleParallel(removeLumberJob, m_LumberQuery, Dependency);
                m_EndFrameBarrier.AddJobHandleForProducer(lumberJobHandle);
            }

            JobHandle woodResourceJobHandle = new ();
            if (!m_WoodResourceQuery.IsEmptyIgnoreFilter)
            {
                FindLumberJob findLumberJob = new ()
                {
                    buffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                    m_WoodResouceType = __TypeHandle.__WoodResource_RO_BufferTypeHandle,
                };
                woodResourceJobHandle = JobChunkExtensions.ScheduleParallel(findLumberJob, m_WoodResourceQuery, lumberJobHandle);
                m_EndFrameBarrier.AddJobHandleForProducer(woodResourceJobHandle);
            }

            Dependency = JobHandle.CombineDependencies(Dependency, woodResourceJobHandle, lumberJobHandle);
            Enabled = false;
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
            [ReadOnly]
            public BufferTypeHandle<WoodResource> __WoodResource_RO_BufferTypeHandle;

            public void __AssignHandles(ref SystemState state)
            {
                __Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
                __WoodResource_RO_BufferTypeHandle = state.GetBufferTypeHandle<WoodResource>();
            }
        }

        private struct FindLumberJob : IJobChunk
        {
            public EntityCommandBuffer.ParallelWriter buffer;
            [ReadOnly]
            public BufferTypeHandle<WoodResource> m_WoodResouceType;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                BufferAccessor<WoodResource> woodResourceBufferAcessory = chunk.GetBufferAccessor(ref m_WoodResouceType);

                for (int i = 0; i < chunk.Count; i++)
                {
                    DynamicBuffer<WoodResource> woodResourceDynamicBuffer = woodResourceBufferAcessory[i];
                    foreach (WoodResource woodResource in woodResourceDynamicBuffer)
                    {
                        buffer.AddComponent<Lumber>(unfilteredChunkIndex, woodResource.m_Tree, default);
                    }
                }
            }
        }


        private struct RemoveLumberJob : IJobChunk
        {
            public EntityCommandBuffer.ParallelWriter buffer;
            [ReadOnly]
            public EntityTypeHandle m_EntityType;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entityNativeArray = chunk.GetNativeArray(m_EntityType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    Entity currentEntity = entityNativeArray[i];
                    buffer.RemoveComponent<Lumber>(unfilteredChunkIndex, currentEntity);
                }
            }
        }
    }
}
