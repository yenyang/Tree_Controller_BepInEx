// <copyright file="Evergreen.cs" company="Yenyangs Mods. MIT License">
// Copyright (c) Yenyangs Mods. MIT License. All rights reserved.
// </copyright>

namespace Tree_Controller
{
    using Colossal.Serialization.Entities;
    using Unity.Entities;

    /// <summary>
    /// A component used to filter out Evergreen trees from queries.
    /// </summary>
    public struct Evergreen : IComponentData, IQueryTypeParameter, IEmptySerializable
    {
    }
}