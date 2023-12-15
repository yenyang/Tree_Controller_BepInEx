// <copyright file="ClearTreeControllerTool.cs" company="Yenyangs Mods. MIT License">
// Copyright (c) Yenyangs Mods. MIT License. All rights reserved.
// </copyright>

namespace Tree_Controller.Tools
{
    using Colossal.Logging;
    using Game;
    using Game.Tools;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;

    /// <summary>
    /// Removed Recently Changed Components.
    /// </summary>
    public partial class ClearTreeControllerTool : GameSystemBase
    {
        private EntityQuery m_RecentlyChangedQuery;
        private ToolOutputBarrier m_ToolOutputBarrier;
        private ILog m_Log;
        private TypeHandle __TypeHandle;

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            m_ToolOutputBarrier = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<ToolOutputBarrier>();
            m_Log = TreeControllerMod.Instance.Logger;
            base.OnCreate();

            m_RecentlyChangedQuery = GetEntityQuery(new EntityQueryDesc[]
            {
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<RecentlyChanged>(),
                    },
                },
            });
            RequireForUpdate(m_RecentlyChangedQuery);
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            __TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref CheckedStateRef);

            RemoveRecentlyChangedComponent removeRecentlyChangedComponentJob = new ()
            {
                m_EntityType = __TypeHandle.__Unity_Entities_Entity_TypeHandle,
                buffer = m_ToolOutputBarrier.CreateCommandBuffer().AsParallelWriter(),
            };
            JobHandle jobHandle = removeRecentlyChangedComponentJob.ScheduleParallel(m_RecentlyChangedQuery, Dependency);
            m_ToolOutputBarrier.AddJobHandleForProducer(jobHandle);
            Dependency = jobHandle;
            m_Log.Debug($"{nameof(ClearTreeControllerTool)}.{nameof(OnUpdate)} Removed Recently Changed Components");
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

            public void __AssignHandles(ref SystemState state)
            {
                __Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
            }
        }

        private struct RemoveRecentlyChangedComponent : IJobChunk
        {
            public EntityTypeHandle m_EntityType;
            public EntityCommandBuffer.ParallelWriter buffer;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entityNativeArray = chunk.GetNativeArray(m_EntityType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    Entity currentEntity = entityNativeArray[i];
                    buffer.RemoveComponent<RecentlyChanged>(unfilteredChunkIndex, currentEntity);
                }
            }
        }
    }
}
