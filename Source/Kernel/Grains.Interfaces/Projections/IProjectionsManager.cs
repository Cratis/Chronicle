// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Definitions;

namespace Cratis.Chronicle.Grains.Projections;

/// <summary>
/// Defines a system that is responsible for supervises projections in the system.
/// </summary>
public interface IProjectionsManager : IGrainWithStringKey
{
    /// <summary>
    /// Register a set of <see cref="ProjectionDefinition"/> for the event store it belongs to.
    /// </summary>
    /// <param name="definitions">A collection of <see cref="ProjectionDefinition"/>.</param>
    /// <returns>Async task.</returns>
    Task Register(IEnumerable<ProjectionDefinition> definitions);
}
