// <copyright file="RecentlyChanged.cs" company="Yenyangs Mods. MIT License">
// Copyright (c) Yenyangs Mods. MIT License. All rights reserved.
// </copyright>

namespace Tree_Controller
{
    using Unity.Entities;

    /// <summary>
    /// A component that is used to disable tree growth individually by exluding from the query.
    /// </summary>
    public struct RecentlyChanged : IComponentData, IQueryTypeParameter
    {
    }
}