// <copyright file="TreeObjectDefinitionSystem.cs" company="Yenyangs Mods. MIT License">
// Copyright (c) Yenyangs Mods. MIT License. All rights reserved.
// </copyright>

namespace Tree_Controller.Systems
{
    using System;
    using System.Collections.Generic;
    using Colossal.Entities;
    using Colossal.Logging;
    using Colossal.Serialization.Entities;
    using Game;
    using Game.Common;
    using Game.Objects;
    using Game.Prefabs;
    using Game.Tools;
    using Tree_Controller.Tools;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;
    using UnityEngine;

    /// <summary>
    /// Overrides tree state on placement with object tool based on setting.
    /// </summary>
    public partial class TreeObjectDefinitionSystem : GameSystemBase
    {
        private readonly Dictionary<TreeState, float> BrushTreeStateAges = new ()
        {
            { 0, 0 },
            { TreeState.Teen, ObjectUtils.TREE_AGE_PHASE_CHILD },
            { TreeState.Adult, ObjectUtils.TREE_AGE_PHASE_CHILD + ObjectUtils.TREE_AGE_PHASE_TEEN },
            { TreeState.Elderly, ObjectUtils.TREE_AGE_PHASE_CHILD + ObjectUtils.TREE_AGE_PHASE_TEEN + ObjectUtils.TREE_AGE_PHASE_ADULT },
            { TreeState.Dead, ObjectUtils.TREE_AGE_PHASE_CHILD + ObjectUtils.TREE_AGE_PHASE_TEEN + ObjectUtils.TREE_AGE_PHASE_ADULT + ObjectUtils.TREE_AGE_PHASE_ELDERLY + .00001f },
        };

        private ToolSystem m_ToolSystem;
        private ObjectToolSystem m_ObjectToolSystem;
        private PrefabSystem m_PrefabSystem;
        private EntityQuery m_ObjectDefinitionQuery;
        private TreeControllerTool m_TreeControllerTool;
        private NativeList<Entity> m_VegetationPrefabEntities;
        private ILog m_Log;

        /// <summary>
        /// Initializes a new instance of the <see cref="TreeObjectDefinitionSystem"/> class.
        /// </summary>
        public TreeObjectDefinitionSystem()
        {
        }

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = TreeControllerMod.Instance.Logger;
            m_ToolSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<ToolSystem>();
            m_ObjectToolSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<ObjectToolSystem>();
            m_PrefabSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<PrefabSystem>();
            m_TreeControllerTool = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<TreeControllerTool>();
            m_Log.Info($"[{nameof(TreeObjectDefinitionSystem)}] {nameof(OnCreate)}");

            m_ObjectDefinitionQuery = GetEntityQuery(new EntityQueryDesc[]
            {
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadWrite<CreationDefinition>(),
                        ComponentType.ReadWrite<Game.Tools.ObjectDefinition>(),
                        ComponentType.ReadOnly<Updated>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<Overridden>(),
                    },
                },
            });

            RequireForUpdate(m_ObjectDefinitionQuery);
        }

        /// <inheritdoc/>
        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            m_VegetationPrefabEntities = new NativeList<Entity>(Allocator.Persistent);

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
                m_VegetationPrefabEntities.Add(element.m_Prefab);
            }

            base.OnGameLoadingComplete(purpose, mode);
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            NativeArray<Entity> entities = m_ObjectDefinitionQuery.ToEntityArray(Allocator.Temp);

            foreach (Entity entity in entities)
            {
                if (!EntityManager.TryGetComponent(entity, out CreationDefinition currentCreationDefinition))
                {
                    entities.Dispose();
                    return;
                }

                if (!EntityManager.TryGetComponent(entity, out ObjectDefinition currentObjectDefinition) || !m_VegetationPrefabEntities.Contains(currentCreationDefinition.m_Prefab))
                {
                    entities.Dispose();
                    return;
                }

                Unity.Mathematics.Random random = new ((uint)(Mathf.Abs(currentObjectDefinition.m_Position.x) + Mathf.Abs(currentObjectDefinition.m_Position.z)) * 1000);
                if (TreeControllerMod.Settings.RandomRotation && !m_ObjectToolSystem.brushing)
                {
                    currentObjectDefinition.m_Rotation = Unity.Mathematics.quaternion.RotateY(random.NextFloat(2f * (float)Math.PI));
                }

                Entity prefabEntity = currentCreationDefinition.m_Prefab;

                if (m_ObjectToolSystem.brushing)
                {
                    prefabEntity = m_TreeControllerTool.GetNextPrefabEntity(ref random);
                    if (prefabEntity != Entity.Null)
                    {
                        currentCreationDefinition.m_Prefab = prefabEntity;
                        EntityManager.SetComponentData(entity, currentCreationDefinition);
                    }
                }

                if (!EntityManager.HasComponent(prefabEntity, ComponentType.ReadOnly<TreeData>()))
                {
                    m_Log.Debug($"{nameof(TreeObjectDefinitionSystem)}.{nameof(OnUpdate)} Does not have TreeData component.");
                    EntityManager.SetComponentData(entity, currentObjectDefinition);
                    return;
                }

                TreeState nextTreeState = m_TreeControllerTool.GetNextTreeState(ref random);
                if (BrushTreeStateAges.ContainsKey(nextTreeState))
                {
                    currentObjectDefinition.m_Age = BrushTreeStateAges[nextTreeState];
                }
                else
                {
                    currentObjectDefinition.m_Age = ObjectUtils.TREE_AGE_PHASE_CHILD + ObjectUtils.TREE_AGE_PHASE_TEEN;
                }

                EntityManager.SetComponentData(entity, currentObjectDefinition);
            }

            entities.Dispose();
        }

        /// <inheritdoc/>
        protected override void OnDestroy()
        {
            m_VegetationPrefabEntities.Dispose();
            base.OnDestroy();
        }
    }
}
