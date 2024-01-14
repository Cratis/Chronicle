// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Storage;
using Aksio.Cratis.Kernel.Storage.Changes;
using Aksio.Cratis.Kernel.Storage.EventSequences;
using Aksio.Cratis.Kernel.Storage.Jobs;
using Aksio.Cratis.Kernel.Storage.Keys;
using Aksio.Cratis.Kernel.Storage.Observation;
using Aksio.Cratis.Kernel.Storage.Recommendations;

namespace Aksio.Cratis.Specifications;

/// <summary>
/// Represents an in-memory implementation of <see cref="IEventStoreNamespaceStorage"/>.
/// </summary>
public class EventStoreNamespaceStorageForSpecifications : IEventStoreNamespaceStorage
{
    readonly IEventSequenceStorage _eventSequenceStorage;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStoreNamespaceStorageForSpecifications"/> class.
    /// </summary>
    /// <param name="eventSequenceStorage"><see cref="IEventSequenceStorage"/> to use.</param>
    public EventStoreNamespaceStorageForSpecifications(IEventSequenceStorage eventSequenceStorage)
    {
        _eventSequenceStorage = eventSequenceStorage;
    }

    /// <inheritdoc/>
    public IChangesetStorage Changesets => throw new NotImplementedException();

    /// <inheritdoc/>
    public IJobStorage Jobs => throw new NotImplementedException();

    /// <inheritdoc/>
    public IJobStepStorage JobSteps => throw new NotImplementedException();

    /// <inheritdoc/>
    public IObserverStorage Observers => throw new NotImplementedException();

    /// <inheritdoc/>
    public IFailedPartitionsStorage FailedPartitions => throw new NotImplementedException();

    /// <inheritdoc/>
    public IRecommendationStorage Recommendations => throw new NotImplementedException();

    /// <inheritdoc/>
    public IObserverKeyIndexes ObserverKeyIndexes => throw new NotImplementedException();

    /// <inheritdoc/>
    public IEventSequenceStorage GetEventSequence(EventSequenceId eventSequenceId) => _eventSequenceStorage;
}
