// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Storage.Changes;
using Aksio.Cratis.Kernel.Storage.EventSequences;
using Aksio.Cratis.Kernel.Storage.Jobs;
using Aksio.Cratis.Kernel.Storage.Keys;
using Aksio.Cratis.Kernel.Storage.Observation;
using Aksio.Cratis.Kernel.Storage.Recommendations;

namespace Aksio.Cratis.Kernel.Storage;

/// <summary>
/// Defines the storage for a specific instance of an event store namespace.
/// </summary>
public interface IEventStoreNamespaceStorage
{
    /// <summary>
    /// Gets the <see cref="IEventSequenceStorage"/> for the event store namespace.
    /// </summary>
    IChangesetStorage Changesets { get; }

    /// <summary>
    /// Gets the <see cref="IJobStorage"/> for the event store namespace.
    /// </summary>
    IJobStorage Jobs { get; }

    /// <summary>
    /// Gets the <see cref="IJobStepStorage"/> for the event store namespace.
    /// </summary>
    IJobStepStorage JobSteps { get; }

    /// <summary>
    /// Gets the <see cref="IObserverStorage"/> for the event store namespace.
    /// </summary>
    IObserverStorage Observers { get; }

    /// <summary>
    /// Gets the <see cref="IEventSequenceStorage"/> for the event store namespace.
    /// </summary>
    IFailedPartitionsStorage FailedPartitions { get; }

    /// <summary>
    /// Gets the <see cref="IRecommendationStorage"/> for the event store namespace.
    /// </summary>
    IRecommendationStorage Recommendations { get; }

    /// <summary>
    /// Gets the <see cref="IObserverKeyIndexes"/>  for the event store namespace.
    /// </summary>
    IObserverKeyIndexes ObserverKeyIndexes { get; }

    /// <summary>
    /// Get the <see cref="IEventSequenceStorage"/> for a specific <see cref="EventSequenceId"/>.
    /// </summary>
    /// <param name="eventSequenceId"><see cref="EventSequenceId"/> to get for.</param>
    /// <returns>The <see cref="IEventStoreNamespaceStorage"/> instance.</returns>
    IEventSequenceStorage GetEventSequence(EventSequenceId eventSequenceId);
}
