// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.ProjectionEngine;

/// <summary>
/// Defines a system for managing <see cref="IProjection">projections</see>.
/// </summary>
public interface IProjectionManager
{
    /// <summary>
    /// Register a <see cref="IProjection"/>.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> the projection is for.</param>
    /// <param name="namespace"><see cref="EventStoreNamespaceName"/> the projection is for.</param>
    /// <param name="projection"><see cref="IProjection"/> to register.</param>
    void Register(EventStoreName eventStore, EventStoreNamespaceName @namespace, IProjection projection);

    /// <summary>
    /// Try to get a <see cref="IProjection"/> by <see cref="ProjectionId"/>.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> the projection is for.</param>
    /// <param name="namespace"><see cref="EventStoreNamespaceName"/> the projection is for.</param>
    /// <param name="id"><see cref="ProjectionId"/> of the projection.</param>
    /// <param name="projection">The <see cref="IProjection"/> if it was found, null if not.</param>
    /// <returns>True if it was found, false if not.</returns>
    bool TryGet(EventStoreName eventStore, EventStoreNamespaceName @namespace, ProjectionId id, [NotNullWhen(true)] out IProjection? projection);
}
