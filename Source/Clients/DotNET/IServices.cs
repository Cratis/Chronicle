// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Contracts.EventSequences;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactions;
using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle;

/// <summary>
/// Defines all the Kernel services available.
/// </summary>
public interface IServices
{
    /// <summary>
    /// Gets the <see cref="IEventSequences"/> service.
    /// </summary>
    IEventSequences EventSequences { get; }

    /// <summary>
    /// Gets the <see cref="IEventTypes"/> service.
    /// </summary>
    IEventTypes EventTypes { get; }

    /// <summary>
    /// Gets the <see cref="IObservers"/> service.
    /// </summary>
    IObservers Observers { get; }

    /// <summary>
    /// Gets the <see cref="IReactions"/> service.
    /// </summary>
    IReactions Reactions { get; }

    /// <summary>
    /// Gets the <see cref="IProjections"/> service.
    /// </summary>
    IProjections Projections { get; }
}
