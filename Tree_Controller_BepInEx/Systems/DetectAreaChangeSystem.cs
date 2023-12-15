// <copyright file="DetectAreaChangeSystem.cs" company="Yenyangs Mods. MIT License">
// Copyright (c) Yenyangs Mods. MIT License. All rights reserved.
// </copyright>

namespace Tree_Controller.Systems
{
    using Colossal.Logging;
    using Game;
    using Game.Areas;
    using Game.Common;
    using Unity.Entities;

    /// <summary>
    /// Detects whether an Area has changed.
    /// </summary>
    public partial class DetectAreaChangeSystem : GameSystemBase
    {
        private EntityQuery m_UpdatedAreaQuery;
        private LumberSystem m_LumberSystem;
        private ILog m_Log;

        /// <summary>
        /// Initializes a new instance of the <see cref="DetectAreaChangeSystem"/> class.
        /// </summary>
        public DetectAreaChangeSystem()
        {
        }

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Log = TreeControllerMod.Instance.Logger;
            m_LumberSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<LumberSystem>();
            m_Log.Info($"{nameof(DetectAreaChangeSystem)} created!");

            m_UpdatedAreaQuery = GetEntityQuery(new EntityQueryDesc[]
            {
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Updated>(),
                        ComponentType.ReadOnly<Extractor>(),
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<Deleted>(),
                    },
                },
            });
            RequireForUpdate(m_UpdatedAreaQuery);
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            m_LumberSystem.Enabled = true;
        }
    }
}
