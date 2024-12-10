// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Contracts.Events.Constraints;
using Cratis.Chronicle.Contracts.EventSequences;
using Cratis.Chronicle.Contracts.Jobs;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle;

/// <summary>
/// Defines all the Kernel services available.
/// </summary>
public interface IServices
{
    /// <summary>
    /// Gets the <see cref="IEventStores"/> service.
    /// </summary>
    IEventStores EventStores { get; }

    /// <summary>
    /// Gets the <see cref="INamespaces"/>  service.
    /// </summary>
    INamespaces Namespaces { get; }

    /// <summary>
    /// Gets the <see cref="IEventSequences"/> service.
    /// </summary>
    IEventSequences EventSequences { get; }

    /// <summary>
    /// Gets the <see cref="IEventTypes"/> service.
    /// </summary>
    IEventTypes EventTypes { get; }

    /// <summary>
    /// Gets the <see cref="IConstraints"/> service.
    /// </summary>
    IConstraints Constraints { get; }

    /// <summary>
    /// Gets the <see cref="IObservers"/> service.
    /// </summary>
    IObservers Observers { get; }

    /// <summary>
    /// Gets the <see cref="IReactors"/> service.
    /// </summary>
    IReactors Reactors { get; }

    /// <summary>
    /// Gets the <see cref="IReducers"/> service.
    /// </summary>
    IReducers Reducers { get; }

    /// <summary>
    /// Gets the <see cref="IProjections"/> service.
    /// </summary>
    IProjections Projections { get; }

    /// <summary>
    /// Gets the <see cref="IJobs"/> service.
    /// </summary>
    IJobs Jobs { get; }
}
