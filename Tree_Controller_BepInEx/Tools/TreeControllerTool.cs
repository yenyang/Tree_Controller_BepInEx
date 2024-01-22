// <copyright file="TreeControllerTool.cs" company="Yenyangs Mods. MIT License">
// Copyright (c) Yenyangs Mods. MIT License. All rights reserved.
// </copyright>

// #define VERBOSE
namespace Tree_Controller.Tools
{
    using System;
    using System.Collections.Generic;
    using Colossal.Annotations;
    using Colossal.Entities;
    using Colossal.Logging;
    using Colossal.Serialization.Entities;
    using Game;
    using Game.Common;
    using Game.Input;
    using Game.Net;
    using Game.Objects;
    using Game.Prefabs;
    using Game.Rendering;
    using Game.Tools;
    using Tree_Controller;
    using Tree_Controller.Settings;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.InputSystem;

    /// <summary>
    /// Tool for controlling tree state or prefab.
    /// </summary>
    public partial class TreeControllerTool : ToolBaseSystem
    {
        private readonly Dictionary<TreeState, float> AgeWeights = new ()
        {
            { 0, ObjectUtils.TREE_AGE_PHASE_CHILD },
            { TreeState.Teen,  ObjectUtils.TREE_AGE_PHASE_TEEN },
            { TreeState.Adult, ObjectUtils.TREE_AGE_PHASE_ADULT },
            { TreeState.Elderly, ObjectUtils.TREE_AGE_PHASE_ELDERLY },
            { TreeState.Dead, ObjectUtils.TREE_AGE_PHASE_DEAD },
        };

        private ProxyAction m_ApplyAction;
        private ProxyAction m_SecondaryApplyAction;
        private OverlayRenderSystem m_OverlayRenderSystem;
        private ToolOutputBarrier m_ToolOutputBarrier;
        private EntityQuery m_TreeQuery;
        private EntityQuery m_TreePrefabQuery;
        private JobHandle treePrefabJobHandle;
        private NativeList<Entity> m_TreePrefabEntities;
        private NativeList<TreeState> m_SelectedTreeStates;
        private NativeList<Entity> m_SelectedTreePrefabEntities;
        [CanBeNull]
        private PrefabBase m_OriginallySelectedPrefab;
        private TCSelectionMode m_SelectionMode = TCSelectionMode.Radius;
        private float m_Radius = 100f;
        private bool m_AtLeastOneAgeSelected = true;
        private ILog m_Log;
        private TypeHandle __TypeHandle;
        private TreeControllerUISystem m_TreeControllerUISystem;

        /// <summary>
        /// An enum for the tool mod selection.
        /// </summary>
        public enum TCSelectionMode
        {
            /// <summary>
            /// Tree Controller Tool will only apply to one tree either on the map, inside a net or building.
            /// </summary>
            SingleTree,

            /// <summary>
            /// Tree Controller Tool will apply to all trees inside a net or building.
            /// </summary>
            WholeBuildingOrNet,

            /// <summary>
            /// Tree controller tool will apply to all trees on the map, inside a net, or building within specified radius.
            /// </summary>
            Radius,

            /// <summary>
            /// Tree controller tool will apply to all trees on the map, inside all nets, and inside all buildings.
            /// </summary>
            WholeMap,
        }


        /// <inheritdoc/>
        public override string toolID => "Tree Controller Tool";

        /// <summary>
        /// Gets or sets the TreeAgeChanger ToolMode.
        /// </summary>
        public TCSelectionMode SelectionMode { get => m_SelectionMode; set => m_SelectionMode = value; }

        /// <summary>
        /// Gets or sets the TreeAgeChanger Radius.
        /// </summary>
        public float Radius { get => m_Radius; set => m_Radius = value; }

        /// <summary>
        /// Gets or sets the OriginallySelectedPrefab.
        /// </summary>
        public PrefabBase OriginallySelectedPrefab { get => m_OriginallySelectedPrefab; set => m_OriginallySelectedPrefab = value; }

        /// <summary>
        /// Gets a Tree State from Selected tree States. Only random implemented right now.
        /// </summary>
        public TreeState NextTreeState
        {
            get
            {
                m_Log.Debug($"{nameof(TreeControllerTool)}.{nameof(NextTreeState)}");
                if (m_SelectedTreeStates.Length == 0)
                {
                    m_Log.Debug($"{nameof(TreeControllerTool)}.{nameof(NextTreeState)} selected tree states.length == 0");
                    return TreeState.Adult;
                }

                switch (TreeControllerMod.Settings.AgeSelectionTechnique)
                {
                    case TreeControllerSettings.AgeSelectionOptions.RandomEqualWeight:
                        Unity.Mathematics.Random random = new ((uint)UnityEngine.Random.Range(1, 100000));
                        return m_SelectedTreeStates[random.NextInt(m_SelectedTreeStates.Length)];

                    case TreeControllerSettings.AgeSelectionOptions.RandomWeighted:
                        float totalWeight = 0f;
                        for (int i = 0; i < m_SelectedTreeStates.Length; i++)
                        {
                            if (AgeWeights.ContainsKey(m_SelectedTreeStates[i]))
                            {
                                totalWeight += AgeWeights[m_SelectedTreeStates[i]];
                            }
                        }

                        Unity.Mathematics.Random random2 = new ((uint)UnityEngine.Random.Range(1, 100000));
                        float randomWeight = random2.NextFloat(totalWeight);
                        float currentWeight = 0f;
                        for (int i = 0; i < m_SelectedTreeStates.Length; i++)
                        {
                            if (AgeWeights.ContainsKey(m_SelectedTreeStates[i]))
                            {
                                currentWeight += AgeWeights[m_SelectedTreeStates[i]];
                            }

                            if (randomWeight < currentWeight)
                            {
                                return m_SelectedTreeStates[i];
                            }
                        }

                        return m_SelectedTreeStates[m_SelectedTreeStates.Length - 1];
                }

                return TreeState.Adult;
            }
        }

        /// <summary>
        /// Adds the selected Prefab to the list by finding prefab entity.
        /// </summary>
        /// <param name="prefab">PrefabBase from object tool.</param>
        public void SelectTreePrefab(PrefabBase prefab)
        {
            Entity prefabEntity = m_PrefabSystem.GetEntity(prefab);
            if (EntityManager.HasComponent<TreeData>(prefabEntity) && !EntityManager.HasComponent<PlaceholderObjectElement>(prefabEntity))
            {
                m_SelectedTreePrefabEntities.Add(prefabEntity);
                if (m_OriginallySelectedPrefab == null)
                {
                    m_OriginallySelectedPrefab = prefab;
                }

                m_Log.Debug($"{nameof(TreeControllerTool)}.{nameof(SelectTreePrefab)} prefabEntity = {prefabEntity.Index}.{prefabEntity.Version}");
            }
        }

        /// <summary>
        /// Resets the selected Tree Prefabs.
        /// </summary>
        public void ClearSelectedTreePrefabs()
        {
            m_SelectedTreePrefabEntities.Clear();
            m_OriginallySelectedPrefab = null;
        }

        /// <summary>
        /// A way for to apply the selected age selection.
        /// </summary>
        /// <param name="ages">An array of ages for selected trees. </param>
        public void ApplySelectedAges(bool[] ages)
        {
            m_SelectedTreeStates.Clear();
            if (CheckForAtLeastOneSelectedAge(ages, ref m_SelectedTreeStates))
            {
                m_AtLeastOneAgeSelected = true;
            }
            else
            {
                m_AtLeastOneAgeSelected = false;
            }
        }

        /// <summary>
        /// Gets the selected ages for export to JS.
        /// </summary>
        /// <returns>An array of bools to represent selected ages.</returns>
        public bool[] GetSelectedAges()
        {
            bool[] ages = new bool[5];
            for (int i = 0; i < m_SelectedTreeStates.Length; i++)
            {
                TreeState state = m_SelectedTreeStates[i];
                if (state == 0)
                {
                    ages[0] = true;
                }
                else
                {
                    ages[(int)Math.Log((double)state, 2.0) + 1] = true;
                }
            }

            return ages;
        }

        /// <inheritdoc/>
        public override PrefabBase GetPrefab()
        {
            if (m_SelectedTreePrefabEntities.Length > 0 && m_OriginallySelectedPrefab != null)
            {
                return m_OriginallySelectedPrefab;
            }

            return null;
        }

        /// <inheritdoc/>
        public override bool TrySetPrefab(PrefabBase prefab)
        {
            if (m_ToolSystem.activeTool != this)
            {
                return false;
            }

            Entity prefabEntity = m_PrefabSystem.GetEntity(prefab);
            if (EntityManager.HasComponent<TreeData>(prefabEntity) && !EntityManager.HasComponent<PlaceholderObjectElement>(prefabEntity))
            {
                if (!Keyboard.current[Key.LeftCtrl].isPressed)
                {
                    ClearSelectedTreePrefabs();
                    m_TreeControllerUISystem.ResetPrefabSets();
                }

                m_ToolSystem.EventPrefabChanged?.Invoke(prefab);
                SelectTreePrefab(prefab);
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public override void InitializeRaycast()
        {
            base.InitializeRaycast();
            if (m_SelectionMode == TCSelectionMode.SingleTree)
            {
                m_ToolRaycastSystem.typeMask = TypeMask.StaticObjects | TypeMask.Net;
                m_ToolRaycastSystem.netLayerMask = Layer.Road | Layer.PublicTransportRoad;
            }
            else if (m_SelectionMode == TCSelectionMode.WholeBuildingOrNet)
            {
                m_ToolRaycastSystem.typeMask = TypeMask.StaticObjects | TypeMask.Net | TypeMask.Terrain;
                m_ToolRaycastSystem.netLayerMask = Layer.Road | Layer.PublicTransportRoad;
            }
            else if (m_SelectionMode == TCSelectionMode.Radius)
            {
                m_ToolRaycastSystem.typeMask = TypeMask.Terrain;
            }
            else if (m_SelectionMode == TCSelectionMode.WholeMap)
            {
                m_ToolRaycastSystem.typeMask = TypeMask.Terrain;
            }
        }

        /// <summary>
        /// For stopping the tool. Probably with esc key.
        /// </summary>
        public void RequestDisable()
        {
            m_ToolSystem.activeTool = m_DefaultToolSystem;
        }

        /// <summary>
        /// Gets a tree state from the selected tree state given a random parameter.
        /// </summary>
        /// <param name="random">A source of randomness.</param>
        /// <returns>A random tree state from selected or Adult if none are selected.</returns>
        public TreeState GetNextTreeState(ref Unity.Mathematics.Random random)
        {
            if (m_SelectedTreeStates.Length == 0)
            {
                return TreeState.Adult;
            }

            switch (TreeControllerMod.Settings.AgeSelectionTechnique)
            {
                case TreeControllerSettings.AgeSelectionOptions.RandomEqualWeight:
                    return m_SelectedTreeStates[random.NextInt(m_SelectedTreeStates.Length)];

                case TreeControllerSettings.AgeSelectionOptions.RandomWeighted:
                    float totalWeight = 0f;
                    for (int i = 0; i < m_SelectedTreeStates.Length; i++)
                    {
                        if (AgeWeights.ContainsKey(m_SelectedTreeStates[i]))
                        {
                            totalWeight += AgeWeights[m_SelectedTreeStates[i]];
                        }
                    }

                    float randomWeight = random.NextFloat(totalWeight);
                    float currentWeight = 0f;
                    for (int i = 0; i < m_SelectedTreeStates.Length; i++)
                    {
                        if (AgeWeights.ContainsKey(m_SelectedTreeStates[i]))
                        {
                            currentWeight += AgeWeights[m_SelectedTreeStates[i]];
                        }

                        if (randomWeight < currentWeight)
                        {
                            return m_SelectedTreeStates[i];
                        }
                    }

                    return m_SelectedTreeStates[m_SelectedTreeStates.Length - 1];
            }

            return TreeState.Adult;
        }

        /// <summary>
        /// Gets a prefab entity from the selected tree prefabs given a random parameter.
        /// </summary>
        /// <param name="random">A source of randomness.</param>
        /// <returns>A random prefab entity from selected or Enity.null.</returns>
        public Entity GetNextPrefabEntity(ref Unity.Mathematics.Random random)
        {
            if (m_SelectedTreePrefabEntities.Length > 0)
            {
                for (int i = 0; i < random.NextInt(5); i++)
                {
                    random.NextInt();
                }

                return m_SelectedTreePrefabEntities[random.NextInt(m_SelectedTreePrefabEntities.Length)];
            }

            return Entity.Null;
        }

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            Enabled = false;
            m_Log = TreeControllerMod.Instance.Logger;
            m_ApplyAction = InputManager.instance.FindAction("Tool", "Apply");
            m_SecondaryApplyAction = InputManager.instance.FindAction("Tool", "Secondary Apply");
            m_Log.Info($"[{nameof(TreeControllerTool)}] {nameof(OnCreate)}");
            m_ToolOutputBarrier = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<ToolOutputBarrier>();
            m_OverlayRenderSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<OverlayRenderSystem>();
            m_TreeControllerUISystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<TreeControllerUISystem>();
            m_SelectedTreeStates = new NativeList<TreeState>(1, Allocator.Persistent)
            {
                TreeState.Adult,
            };
            m_SelectedTreePrefabEntities = new NativeList<Entity>(0, Allocator.Persistent);
            InputAction hotKey = new ("TreeControllerTool");
            hotKey.AddCompositeBinding("ButtonWithOneModifier").With("Modifier", "<Keyboard>/ctrl").With("Button", "<Keyboard>/t");
            hotKey.performed += this.OnKeyPressed;
            hotKey.Enable();
            base.OnCreate();

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
                    },
                },
            });
            m_TreeQuery = GetEntityQuery(new EntityQueryDesc[]
            {
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadWrite<Tree>(),
                        ComponentType.ReadOnly<Game.Objects.Transform>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadOnly<Overridden>(),
                        ComponentType.ReadOnly<RecentlyChanged>(),
                    },
                },
            });
            RequireForUpdate(m_TreeQuery);
            RequireForUpdate(m_TreePrefabQuery);
        }

        /// <inheritdoc/>
        protected override void OnStartRunning()
        {
            m_ApplyAction.shouldBeEnabled = true;
            m_SecondaryApplyAction.shouldBeEnabled = true;
            m_Log.Debug($"{nameof(TreeControllerTool)}.{nameof(OnStartRunning)}");
        }

        /// <inheritdoc/>
        protected override void OnStopRunning()
        {
            m_ApplyAction.shouldBeEnabled = false;
            m_SecondaryApplyAction.shouldBeEnabled = false;
        }

        /// <inheritdoc/>
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            m_TreePrefabEntities = new (0, Allocator.Temp);

            __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle.Update(ref CheckedStateRef);
            __TypeHandle.__Game_Object_Tree_RW_ComponentTypeHandle.Update(ref CheckedStateRef);
            __TypeHandle.__Unity_Entities_Entity_TypeHandle.Update(ref CheckedStateRef);
            __TypeHandle.__PrefabRef_RW_ComponentTypeHandle.Update(ref CheckedStateRef);

            if (!m_TreePrefabQuery.IsEmptyIgnoreFilter)
            {
                m_TreePrefabEntities = m_TreePrefabQuery.ToEntityListAsync(Allocator.Persistent, out treePrefabJobHandle);
            }

            inputDeps = Dependency;
            bool raycastFlag = GetRaycastResult(out Entity e, out RaycastHit hit);
            bool hasTreeComponentFlag = EntityManager.HasComponent<Game.Objects.Tree>(e);
            bool hasTransformComponentFlag = EntityManager.HasComponent<Game.Objects.Transform>(e);
            bool hasBufferFlag = EntityManager.HasBuffer<Game.Objects.SubObject>(e);

            if (m_Radius > 0 && raycastFlag && m_SelectionMode == TCSelectionMode.Radius) // Radius Circle
            {
                ToolRadiusJob toolRadiusJob = new ()
                {
                    m_OverlayBuffer = m_OverlayRenderSystem.GetBuffer(out JobHandle outJobHandle),
                    m_Position = new Vector3(hit.m_HitPosition.x, hit.m_Position.y, hit.m_HitPosition.z),
                    m_Radius = m_Radius,
                };
                inputDeps = IJobExtensions.Schedule(toolRadiusJob, JobHandle.CombineDependencies(inputDeps, outJobHandle));
                m_OverlayRenderSystem.AddBufferWriter(inputDeps);
            }

            if (m_ApplyAction.WasPressedThisFrame())
            {
                if (m_SelectionMode == TCSelectionMode.SingleTree || m_SelectionMode == TCSelectionMode.WholeBuildingOrNet)
                {
                    if (raycastFlag && hasTreeComponentFlag)
                    {
                        if (m_AtLeastOneAgeSelected)
                        {
                            ChangeTreeStateJob changeTreeStateJob = new ()
                            {
                                m_Entity = e,
                                m_Random = new ((uint)UnityEngine.Random.Range(1, 100000)),
                                m_Ages = m_SelectedTreeStates,
                                m_Tree = EntityManager.GetComponentData<Tree>(e),
                                buffer = m_ToolOutputBarrier.CreateCommandBuffer(),
                            };
                            inputDeps = changeTreeStateJob.Schedule(inputDeps);
                            m_ToolOutputBarrier.AddJobHandleForProducer(inputDeps);
                        }

                        bool doNotApplyTreePrefab = false;
                        if (EntityManager.TryGetComponent<PrefabRef>(e, out PrefabRef prefabRef))
                        {
                            if (m_PrefabSystem.TryGetPrefab(prefabRef, out PrefabBase prefabBase))
                            {
                                if (prefabBase.GetType() == typeof(RoadPrefab))
                                {
                                    doNotApplyTreePrefab = true;
                                }
                            }
                        }

                        if (!m_SelectedTreePrefabEntities.IsEmpty && !doNotApplyTreePrefab)
                        {
                            ChangePrefabRefJob changePrefabRefJob = new ()
                            {
                                m_Entity = e,
                                m_SelectedPrefabEntities = m_SelectedTreePrefabEntities,
                                m_Random = new ((uint)UnityEngine.Random.Range(1, 100000)),
                                buffer = m_ToolOutputBarrier.CreateCommandBuffer(),
                            };
                            inputDeps = changePrefabRefJob.Schedule(JobHandle.CombineDependencies(inputDeps, treePrefabJobHandle));
                            m_ToolOutputBarrier.AddJobHandleForProducer(inputDeps);
                        }
                    }
                    else if (raycastFlag)
                    {
                        ProcessBufferForTrees(e, hit, ref inputDeps); // Loops through buffer in Static Object with Subobjects and changes tree age/prefab for all subobject trees within radius.
                    }
                }
            }
            else if (m_ApplyAction.IsPressed() && m_SelectionMode == TCSelectionMode.Radius && raycastFlag)
            {
                bool overridePrefab = !m_SelectedTreePrefabEntities.IsEmpty;
                if (m_AtLeastOneAgeSelected || overridePrefab)
                {
                    TreeChangerWithinRadius changeTreeAgeWithinRadiusJob = new ()
                    {
                        m_EntityType = __TypeHandle.__Unity_Entities_Entity_TypeHandle,
                        m_Position = hit.m_HitPosition,
                        m_Radius = m_Radius,
                        m_Ages = m_SelectedTreeStates,
                        m_TransformType = __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle,
                        m_TreeType = __TypeHandle.__Game_Object_Tree_RW_ComponentTypeHandle,
                        buffer = m_ToolOutputBarrier.CreateCommandBuffer().AsParallelWriter(),
                        m_PrefabRefType = __TypeHandle.__PrefabRef_RW_ComponentTypeHandle,
                        m_OverrideState = m_AtLeastOneAgeSelected,
                        m_OverridePrefab = overridePrefab,
                        m_Random = new ((uint)UnityEngine.Random.Range(1, 100000)),
                        m_PrefabEntities = m_SelectedTreePrefabEntities,
                    };
                    inputDeps = JobChunkExtensions.ScheduleParallel(changeTreeAgeWithinRadiusJob, m_TreeQuery, JobHandle.CombineDependencies(inputDeps, treePrefabJobHandle));
                    m_ToolOutputBarrier.AddJobHandleForProducer(inputDeps);
                }
            }
            else if (m_SecondaryApplyAction.WasPressedThisFrame() && m_SelectionMode == TCSelectionMode.WholeMap && raycastFlag)
            {
                bool overridePrefab = !m_SelectedTreePrefabEntities.IsEmpty;
                if (m_AtLeastOneAgeSelected || overridePrefab)
                {
                    TreeChangerWholeMap changeTreeAgeWholeMap = new ()
                    {
                        m_EntityType = __TypeHandle.__Unity_Entities_Entity_TypeHandle,
                        m_Ages = m_SelectedTreeStates,
                        m_TreeType = __TypeHandle.__Game_Object_Tree_RW_ComponentTypeHandle,
                        buffer = m_ToolOutputBarrier.CreateCommandBuffer().AsParallelWriter(),
                        m_PrefabRefType = __TypeHandle.__PrefabRef_RW_ComponentTypeHandle,
                        m_OverrideState = m_AtLeastOneAgeSelected,
                        m_OverridePrefab = overridePrefab,
                        m_Random = new ((uint)UnityEngine.Random.Range(1, 100000)),
                        m_PrefabEntities = m_SelectedTreePrefabEntities,
                    };
                    inputDeps = JobChunkExtensions.ScheduleParallel(changeTreeAgeWholeMap, m_TreeQuery, JobHandle.CombineDependencies(inputDeps, treePrefabJobHandle));
                    m_ToolOutputBarrier.AddJobHandleForProducer(inputDeps);
                }
            }
            else if (raycastFlag && hasTreeComponentFlag && hasTransformComponentFlag) // Single Tree Circle
            {
                TreeCircleRenderJob treeCircleRenderJob = new ()
                {
                    m_OverlayBuffer = m_OverlayRenderSystem.GetBuffer(out JobHandle outJobHandle),
                    m_Transform = EntityManager.GetComponentData<Game.Objects.Transform>(e),
                };
                inputDeps = IJobExtensions.Schedule(treeCircleRenderJob, JobHandle.CombineDependencies(inputDeps, outJobHandle));
                m_OverlayRenderSystem.AddBufferWriter(inputDeps);
            }
            else if (raycastFlag && hasBufferFlag) // Subobject Circles
            {
                if (EntityManager.TryGetBuffer(e, isReadOnly: true, out DynamicBuffer<Game.Objects.SubObject> buffer))
                {
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        Entity subObject = buffer[i].m_SubObject;
                        if (EntityManager.HasComponent<Tree>(subObject) && EntityManager.HasComponent<Game.Objects.Transform>(subObject))
                        {
                            Game.Objects.Transform currentTransform = EntityManager.GetComponentData<Game.Objects.Transform>(subObject);
                            float radius = 2f;
                            if (m_SelectionMode == TCSelectionMode.Radius)
                            {
                                radius = m_Radius;
                            }

                            if (CheckForHoveringOverTree(new Vector3(hit.m_HitPosition.x, hit.m_Position.y, hit.m_HitPosition.z), currentTransform.m_Position, radius) || m_SelectionMode == TCSelectionMode.WholeBuildingOrNet)
                            {
                                TreeCircleRenderJob treeCircleRenderJob = new ()
                                {
                                    m_OverlayBuffer = m_OverlayRenderSystem.GetBuffer(out JobHandle outJobHandle),
                                    m_Transform = currentTransform,
                                };
                                inputDeps = IJobExtensions.Schedule(treeCircleRenderJob, JobHandle.CombineDependencies(inputDeps, outJobHandle));
                                m_OverlayRenderSystem.AddBufferWriter(inputDeps);
                            }
                        }
                    }
                }
            }

            if (m_ApplyAction.WasReleasedThisFrame() && m_SelectionMode == TCSelectionMode.Radius)
            {
                applyMode = ApplyMode.Clear;
            }
            else if (applyMode == ApplyMode.Clear)
            {
                applyMode = ApplyMode.None;
            }

            m_TreePrefabEntities.Dispose(inputDeps);
            return inputDeps;
        }

        /// <inheritdoc/>
        protected override void OnGameLoaded(Context serializationContext)
        {
            base.OnGameLoaded(serializationContext);
        }

        /// <inheritdoc/>
        protected override void OnGamePreload(Purpose purpose, GameMode mode)
        {
            base.OnGamePreload(purpose, mode);
        }

        /// <inheritdoc/>
        protected override void OnDestroy()
        {
            base.OnDestroy();
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
            public ComponentTypeHandle<Tree> __Game_Object_Tree_RW_ComponentTypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;
            public ComponentTypeHandle<PrefabRef> __PrefabRef_RW_ComponentTypeHandle;

            public void __AssignHandles(ref SystemState state)
            {
                __Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
                __Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.Transform>();
                __Game_Object_Tree_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Tree>();
                __PrefabRef_RW_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>();
            }
        }

        /// <summary>
        /// Add keybinding to tool so you can enabled tool in game.
        /// </summary>
        private void OnKeyPressed(InputAction.CallbackContext context)
        {
            if (m_ToolSystem.activeTool != this && m_ToolSystem.activeTool == m_DefaultToolSystem)
            {
                m_ToolSystem.selected = Entity.Null;
                m_ToolSystem.activeTool = this;
            }
        }

        /// <summary>
        /// Checks selected ages for at least one selection.
        /// </summary>
        /// <param name="ages">An array of bools for the selected ages.</param>
        /// <returns>True if at least one age is true. False if they are all false.</returns>
        private bool CheckForAtLeastOneSelectedAge(bool[] ages, ref NativeList<TreeState> states)
        {
            bool flag = false;
            for (int i = 0; i < ages.Length; i++)
            {
                if (ages[i])
                {
                    flag = true;
                    TreeState state = (TreeState)(int)Math.Pow(2, i - 1);
                    states.Add(state);
                }
            }

            if (flag)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Will loop through subobjects of the entity and change tree state. In future will change prefab.
        /// </summary>
        /// <param name="e">Entity that was hit by raycast.</param>
        /// <param name="hit">Raycast information.</param>
        /// <param name="jobHandle">So input deps can be passed along.</param>
        private void ProcessBufferForTrees(Entity e, RaycastHit hit, ref JobHandle jobHandle)
        {
            if (EntityManager.TryGetBuffer(e, isReadOnly: true, out DynamicBuffer<Game.Objects.SubObject> buffer))
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    Entity subObject = buffer[i].m_SubObject;
                    if (EntityManager.HasComponent<Tree>(subObject) && EntityManager.HasComponent<Game.Objects.Transform>(subObject))
                    {
                        Game.Objects.Transform currentTransform = EntityManager.GetComponentData<Game.Objects.Transform>(subObject);
                        if (CheckForHoveringOverTree(new Vector3(hit.m_HitPosition.x, hit.m_Position.y, hit.m_HitPosition.z), currentTransform.m_Position, 2f) || m_SelectionMode == TCSelectionMode.WholeBuildingOrNet)
                        {
                            if (m_AtLeastOneAgeSelected)
                            {
                                ChangeTreeStateJob changeTreeStateJob = new()
                                {
                                    m_Entity = subObject,
                                    m_Random = new ((uint)UnityEngine.Random.Range(1, 100000)),
                                    m_Ages = m_SelectedTreeStates,
                                    m_Tree = EntityManager.GetComponentData<Tree>(subObject),
                                    buffer = m_ToolOutputBarrier.CreateCommandBuffer(),
                                };
                                jobHandle = changeTreeStateJob.Schedule(JobHandle.CombineDependencies(jobHandle, treePrefabJobHandle));
                                m_ToolOutputBarrier.AddJobHandleForProducer(jobHandle);
                            }

                            bool doNotApplyTreePrefab = false;
                            if (EntityManager.TryGetComponent<PrefabRef>(e, out PrefabRef prefabRef))
                            {
                                if (m_PrefabSystem.TryGetPrefab(prefabRef, out PrefabBase prefabBase))
                                {
                                    if (prefabBase.GetType() == typeof(RoadPrefab))
                                    {
                                        doNotApplyTreePrefab = true;
                                    }
                                }
                            }

                            if (!m_SelectedTreePrefabEntities.IsEmpty && !doNotApplyTreePrefab)
                            {
                                ChangePrefabRefJob changePrefabRefJob = new()
                                {
                                    m_Entity = subObject,
                                    m_SelectedPrefabEntities = m_SelectedTreePrefabEntities,
                                    m_Random = new((uint)UnityEngine.Random.Range(1, 100000)),
                                    buffer = m_ToolOutputBarrier.CreateCommandBuffer(),
                                };
                                jobHandle = changePrefabRefJob.Schedule(JobHandle.CombineDependencies(jobHandle, treePrefabJobHandle));
                                m_ToolOutputBarrier.AddJobHandleForProducer(jobHandle);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Compares position of cursor and tree with regrads to a given radius.
        /// </summary>
        /// <param name="cursorPosition">From the raycast hit.</param>
        /// <param name="treePosition">Transform from the tree.</param>
        /// <param name="radius">The radius for comparison.</param>
        /// <returns>True if tree position is whithin radius from cursor position.</returns>
        private bool CheckForHoveringOverTree(float3 cursorPosition, float3 treePosition, float radius)
        {
            float minRadius = 1f;
            radius = Mathf.Max(radius, minRadius);
            float2 cursorPositionXZ = new (cursorPosition.x, cursorPosition.z);
            float2 treePositionXZ = new (treePosition.x, treePosition.z);
            if (Unity.Mathematics.math.distance(cursorPositionXZ, treePositionXZ) < radius)
            {
                return true;
            }

            return false;
        }

        private struct TreeChangerWithinRadius : IJobChunk
        {
            public EntityTypeHandle m_EntityType;
            public ComponentTypeHandle<Game.Objects.Tree> m_TreeType;
            public ComponentTypeHandle<Game.Objects.Transform> m_TransformType;
            public bool m_OverrideState;
            public bool m_OverridePrefab;
            public NativeList<TreeState> m_Ages;
            public EntityCommandBuffer.ParallelWriter buffer;
            public ComponentTypeHandle<Game.Prefabs.PrefabRef> m_PrefabRefType;
            public NativeList<Entity> m_PrefabEntities;
            public Unity.Mathematics.Random m_Random;
            public float m_Radius;
            public float3 m_Position;

            /// <summary>
            /// Executes job which will change state or prefab for trees within a radius.
            /// </summary>
            /// <param name="chunk">ArchteypeChunk of IJobChunk.</param>
            /// <param name="unfilteredChunkIndex">Use for EntityCommandBuffer.ParralelWriter.</param>
            /// <param name="useEnabledMask">Part of IJobChunk. Unsure what it does.</param>
            /// <param name="chunkEnabledMask">Part of IJobChunk. Not sure what it does.</param>
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entityNativeArray = chunk.GetNativeArray(m_EntityType);
                NativeArray<Game.Objects.Transform> transformNativeArray = chunk.GetNativeArray(ref m_TransformType);
                NativeArray<Game.Objects.Tree> treeNativeArray = chunk.GetNativeArray(ref m_TreeType);
                NativeArray<Game.Prefabs.PrefabRef> prefabRefNativeArray = chunk.GetNativeArray(ref m_PrefabRefType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    if (CheckForHoveringOverTree(m_Position, transformNativeArray[i].m_Position, m_Radius))
                    {
                        Entity currentEntity = entityNativeArray[i];
                        bool addBatchesUpdated = false;
                        if (m_OverrideState)
                        {
                            Game.Objects.Tree currentTreeData = treeNativeArray[i];
                            currentTreeData.m_State = GetTreeState(m_Ages, currentTreeData);
                            buffer.SetComponent(unfilteredChunkIndex, currentEntity, currentTreeData);
                            buffer.AddComponent<RecentlyChanged>(unfilteredChunkIndex, currentEntity);
                            addBatchesUpdated = true;
                        }

                        if (m_OverridePrefab == true)
                        {
                            PrefabRef currentPrefabRef = prefabRefNativeArray[i];

                            if (m_PrefabEntities.Length > 0)
                            {
                                currentPrefabRef.m_Prefab = m_PrefabEntities[m_Random.NextInt(m_PrefabEntities.Length)];
                            }
                            else
                            {
                                continue;
                            }

                            buffer.SetComponent(unfilteredChunkIndex, currentEntity, currentPrefabRef);
                            buffer.RemoveComponent<Evergreen>(unfilteredChunkIndex, currentEntity);
                            buffer.RemoveComponent<DeciduousData>(unfilteredChunkIndex, currentEntity);
                            buffer.AddComponent<RecentlyChanged>(unfilteredChunkIndex, currentEntity);
                            buffer.AddComponent<Updated>(unfilteredChunkIndex, currentEntity);
                            addBatchesUpdated = true;
                        }

                        if (addBatchesUpdated)
                        {
                            buffer.AddComponent<BatchesUpdated>(unfilteredChunkIndex, currentEntity);
                        }
                    }
                }
            }

            /// <summary>
            /// Checks the radius and position and returns true if tree is there.
            /// </summary>
            /// <param name="cursorPosition">Float3 from Raycast.</param>
            /// <param name="treePosition">Float3 position from Transform.</param>
            /// <param name="radius">Radius usually passed from settings.</param>
            /// <returns>True if tree position is within radius of position. False if not.</returns>
            private bool CheckForHoveringOverTree(float3 cursorPosition, float3 treePosition, float radius)
            {
                float minRadius = 5f;
                radius = Mathf.Max(radius, minRadius);
                if (Unity.Mathematics.math.distance(cursorPosition, treePosition) < radius)
                {
                    return true;
                }

                return false;
            }

            /// <summary>
            /// Gets a specified tree state or random tree state from Ages array.
            /// </summary>
            /// <param name="ages">Selected ages.</param>
            /// <param name="tree">Tree so original TreeState can be used if no ages are selected.</param>
            /// <returns>TreeState.</returns>
            private TreeState GetTreeState(NativeList<TreeState> ages, Game.Objects.Tree tree)
            {
                if (ages.Length > 0)
                {
                    TreeState returnState = ages[m_Random.NextInt(ages.Length)];
                    return returnState;
                }

                return tree.m_State;
            }
        }

        private struct TreeChangerWholeMap : IJobChunk
        {
            public EntityTypeHandle m_EntityType;
            public ComponentTypeHandle<Game.Objects.Tree> m_TreeType;
            public bool m_OverrideState;
            public bool m_OverridePrefab;
            public NativeList<TreeState> m_Ages;
            public EntityCommandBuffer.ParallelWriter buffer;
            public ComponentTypeHandle<Game.Prefabs.PrefabRef> m_PrefabRefType;
            public NativeList<Entity> m_PrefabEntities;
            public Unity.Mathematics.Random m_Random;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entityNativeArray = chunk.GetNativeArray(m_EntityType);
                NativeArray<Game.Objects.Tree> treeNativeArray = chunk.GetNativeArray(ref m_TreeType);
                NativeArray<Game.Prefabs.PrefabRef> prefabRefNativeArray = chunk.GetNativeArray(ref m_PrefabRefType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    Entity currentEntity = entityNativeArray[i];
                    bool addBatchesUpdated = false;
                    if (m_OverrideState)
                    {
                        Game.Objects.Tree currentTreeData = treeNativeArray[i];
                        currentTreeData.m_State = GetTreeState(m_Ages, currentTreeData);
                        buffer.SetComponent(unfilteredChunkIndex, currentEntity, currentTreeData);
                        addBatchesUpdated = true;
                    }

                    if (m_OverridePrefab == true)
                    {
                        PrefabRef currentPrefabRef = prefabRefNativeArray[i];
                        if (m_PrefabEntities.Length > 0)
                        {
                            currentPrefabRef.m_Prefab = m_PrefabEntities[m_Random.NextInt(m_PrefabEntities.Length)];
                        }
                        else
                        {
                            continue;
                        }

                        buffer.SetComponent(unfilteredChunkIndex, currentEntity, currentPrefabRef);
                        buffer.RemoveComponent<Evergreen>(unfilteredChunkIndex, currentEntity);
                        buffer.RemoveComponent<DeciduousData>(unfilteredChunkIndex, currentEntity);
                        buffer.AddComponent<Updated>(unfilteredChunkIndex, currentEntity);
                        addBatchesUpdated = true;
                    }

                    if (addBatchesUpdated)
                    {
                        buffer.AddComponent<BatchesUpdated>(unfilteredChunkIndex, currentEntity);
                    }
                }
            }

            /// <summary>
            /// Gets a specified tree state or random tree state from Ages array.
            /// </summary>
            /// <param name="ages">Selected ages.</param>
            /// <param name="tree">Tree so original TreeState can be used if no ages are selected.</param>
            /// <returns>TreeState.</returns>
            private TreeState GetTreeState(NativeList<TreeState> ages, Game.Objects.Tree tree)
            {
                if (ages.Length > 0)
                {
                    TreeState returnState = ages[m_Random.NextInt(ages.Length)];
                    return returnState;
                }

                return tree.m_State;
            }
        }

        private struct TreeCircleRenderJob : IJob
        {
            public OverlayRenderSystem.Buffer m_OverlayBuffer;
            public Game.Objects.Transform m_Transform;

            /// <summary>
            /// Draws circles around trees.
            /// </summary>
            public void Execute()
            {
                m_OverlayBuffer.DrawCircle(new UnityEngine.Color(.88f, .26f, 0.90f), default, 0.25f, 0, new float2(0, 1), m_Transform.m_Position, 3f);
            }
        }

        private struct ToolRadiusJob : IJob
        {
            public OverlayRenderSystem.Buffer m_OverlayBuffer;
            public float3 m_Position;
            public float m_Radius;

            /// <summary>
            /// Draws tool radius.
            /// </summary>
            public void Execute()
            {
                m_OverlayBuffer.DrawCircle(new UnityEngine.Color(.52f, .80f, .86f, 1f), default, m_Radius / 20f, 0, new float2(0, 1), m_Position, m_Radius * 2f);
            }
        }

        private struct ChangePrefabRefJob : IJob
        {
            public Entity m_Entity;
            public NativeList<Entity> m_SelectedPrefabEntities;
            public EntityCommandBuffer buffer;
            public Unity.Mathematics.Random m_Random;

            /// <summary>
            /// Changes prefab ref for specified entity.
            /// </summary>
            public void Execute()
            {
                PrefabRef prefabRef;
                if (!m_SelectedPrefabEntities.IsEmpty)
                {
                    prefabRef = new PrefabRef(m_SelectedPrefabEntities[m_Random.NextInt(m_SelectedPrefabEntities.Length)]);
                    buffer.SetComponent(m_Entity, prefabRef);
                    buffer.RemoveComponent<Evergreen>(m_Entity);
                    buffer.RemoveComponent<DeciduousData>(m_Entity);
                    buffer.AddComponent<Updated>(m_Entity);
                }
            }
        }

        private struct ChangeTreeStateJob : IJob
        {
            public Entity m_Entity;
            public Tree m_Tree;
            public NativeList<TreeState> m_Ages;
            public EntityCommandBuffer buffer;
            public Unity.Mathematics.Random m_Random;

            /// <summary>
            /// Changes TreeState for specfied tree entity.
            /// </summary>
            public void Execute()
            {
                m_Tree.m_State = GetTreeState(m_Ages, m_Tree);
                buffer.SetComponent(m_Entity, m_Tree);
                buffer.AddComponent<BatchesUpdated>(m_Entity);
            }

            /// <summary>
            /// Gets a specified tree state or random tree state from Ages array.
            /// </summary>
            /// <param name="ages">Selected ages.</param>
            /// <param name="tree">Tree so original TreeState can be used if no ages are selected.</param>
            /// <returns>TreeState.</returns>
            private TreeState GetTreeState(NativeList<TreeState> ages, Game.Objects.Tree tree)
            {
                if (ages.Length > 0)
                {
                    TreeState returnState = ages[m_Random.NextInt(ages.Length)];
                    return returnState;
                }

                return tree.m_State;
            }
        }
    }
}
