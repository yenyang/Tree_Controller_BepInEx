// <copyright file="DeciduousData.cs" company="Yenyangs Mods. MIT License">
// Copyright (c) Yenyangs Mods. MIT License. All rights reserved.
// </copyright>

namespace Tree_Controller
{
    using Colossal.Serialization.Entities;
    using Game.Objects;
    using Unity.Entities;

    /// <summary>
    /// A custom component for deciduous trees.
    /// </summary>
    public struct DeciduousData : IComponentData, IQueryTypeParameter, ISerializable
    {
        /// <summary>
        /// During winter the previous tree state is stored here.
        /// </summary>
        public TreeState m_PreviousTreeState;

        /// <summary>
        /// This records whether the tree died naturally or if the tree is just using the dead model during winter.
        /// </summary>
        public bool m_TechnicallyDead;

        /// <summary>
        /// Saves the custom component onto the save file. First item written is the version number.
        /// </summary>
        /// <typeparam name="TWriter">Used by game.</typeparam>
        /// <param name="writer">This is part of the game.</param>
        public void Serialize<TWriter>(TWriter writer)
            where TWriter : IWriter
        {
            writer.Write(1); // Version Number for Component.
            writer.Write((byte)m_PreviousTreeState);
            writer.Write(m_TechnicallyDead);
        }

        /// <summary>
        /// Loads the custom component from the save file. First item read is the version number.
        /// </summary>
        /// <typeparam name="TReader">Used by game.</typeparam>
        /// <param name="reader">This is part of the game.</param>
        public void Deserialize<TReader>(TReader reader)
            where TReader : IReader
        {
            reader.Read(out int version);
            reader.Read(out byte treeState);
            m_PreviousTreeState = (TreeState)treeState;
            reader.Read(out m_TechnicallyDead);
        }
    }
}