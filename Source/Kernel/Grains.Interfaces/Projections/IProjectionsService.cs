// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Orleans.Services;

namespace Cratis.Chronicle.Grains.Projections;

/// <summary>
/// Defines a service that is responsible for projection registration on the silo.
/// </summary>
public interface IProjectionsService : IGrainService
{
    /// <summary>
    /// Register a set of <see cref="ProjectionDefinition"/> for a specific event store.
    /// </summary>
    /// <param name="eventStore">Name of the event store.</param>
    /// <param name="definitions">A collection of <see cref="ProjectionDefinition"/>.</param>
    /// <returns>Awaitable task.</returns>
    Task Register(EventStoreName eventStore, IEnumerable<ProjectionDefinition> definitions);
}
