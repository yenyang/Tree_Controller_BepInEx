// <copyright file="CustomSetRepository.cs" company="Yenyangs Mods. MIT License">
// Copyright (c) Yenyangs Mods. MIT License. All rights reserved.
// </copyright>

namespace Tree_Controller.Settings
{
    using System.Collections.Generic;
    using Game.Prefabs;
    using Unity.Entities;

    /// <summary>
    /// A class to use for XML serialization and deserialization for storing prefabs used in a custom set.
    /// </summary>
    public class CustomSetRepository
    {
        private string[] m_PrefabNames;
        private int m_Version;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomSetRepository"/> class.
        /// </summary>
        public CustomSetRepository()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomSetRepository"/> class.
        /// </summary>
        /// <param name="name">the name for the custom set that corresponds with the name locale key.</param>
        /// <param name="nameLocaleKey">key code for the localization of the custom set name.</param>
        /// <param name="description">the description that corresponds with the description locale key.</param>
        /// <param name="descriptionLocaleKey">key code for the localization of the custom set description.</param>
        /// <param name="customSet">list of prefabBases for the custom set.</param>
        public CustomSetRepository(List<PrefabBase> customSet)
        {
            m_PrefabNames = ConvertToArray(customSet);
            m_Version = 1;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomSetRepository"/> class.
        /// </summary>
        /// <param name="name">the name for the custom set that corresponds with the name locale key.</param>
        /// <param name="nameLocaleKey">key code for the localization of the custom set name.</param>
        /// <param name="description">the description that corresponds with the description locale key.</param>
        /// <param name="descriptionLocaleKey">key code for the localization of the custom set description.</param>
        /// <param name="customSet">list of prefab IDs for the custom set.</param>
        public CustomSetRepository(List<PrefabID> customSet)
        {
            m_PrefabNames = ConvertToArray(customSet);
            m_Version = 1;
        }

        /// <summary>
        /// Gets or sets a value indicating the names of the prefabs in the set.
        /// </summary>
        public string[] PrefabNames
        {
            get { return m_PrefabNames; }
            set { m_PrefabNames = value; }
        }

        /// <summary>
        /// Gets a list of PrefabBases from the array of prefab names.
        /// </summary>
        /// <returns>List of PrefabBases.</returns>
        public List<PrefabBase> GetPrefabBases()
        {
            PrefabSystem prefabSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<PrefabSystem>();
            List<PrefabBase> prefabs = new List<PrefabBase>();
            foreach (string name in m_PrefabNames)
            {
                PrefabID prefabID = new PrefabID("StaticObjectPrefab", name);
                if (prefabSystem.TryGetPrefab(prefabID, out PrefabBase prefab))
                {
                    if (prefab != null)
                    {
                        prefabs.Add(prefab);
                    }
                }
            }

            return prefabs;
        }

        /// <summary>
        /// Gets a list of PrefabBases from the array of prefab names.
        /// </summary>
        /// <returns>List of PrefabBases.</returns>
        public List<PrefabID> GetPrefabIDs()
        {
            List<PrefabID> prefabIDs = new List<PrefabID>();
            foreach (string name in m_PrefabNames)
            {
                PrefabID prefabID = new PrefabID("StaticObjectPrefab", name);
                prefabIDs.Add(prefabID);
            }

            return prefabIDs;
        }

        /// <summary>
        /// Sets m_PrefabNames from a list of prefab IDs.
        /// </summary>
        /// <param name="prefabs">List of prefab IDs for the custom set.</param>
        public void SetPrefabs(List<PrefabBase> prefabs)
        {
            m_PrefabNames = ConvertToArray(prefabs);
        }

        private string[] ConvertToArray(List<PrefabBase> list)
        {
            string[] array = new string[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                array[i] = list[i].name;
            }

            return array;
        }

        private string[] ConvertToArray(List<PrefabID> list)
        {
            string[] array = new string[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                array[i] = list[i].GetName();
            }

            return array;
        }
    }
}
