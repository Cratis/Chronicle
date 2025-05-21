// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Defines a system for handling projections.
/// </summary>
public interface IProjectionHandler : ICanGetObserverInformation
{
    /// <summary>
    /// Gets the <see cref="ProjectionDefinition"/>.
    /// </summary>
    ProjectionDefinition Definition { get; }

    /// <summary>
    /// Gets the current <see cref="ProjectionState"/>.
    /// </summary>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
    [Obsolete("Obsolete")]
    Task<ProjectionState> GetState();
}
