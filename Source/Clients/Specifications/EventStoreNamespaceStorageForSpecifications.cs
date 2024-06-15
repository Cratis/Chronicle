// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Changes;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Jobs;
using Cratis.Chronicle.Storage.Keys;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Chronicle.Storage.Recommendations;
using Cratis.EventSequences;

namespace Cratis.Specifications;

/// <summary>
/// Represents an in-memory implementation of <see cref="IEventStoreNamespaceStorage"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EventStoreNamespaceStorageForSpecifications"/> class.
/// </remarks>
/// <param name="eventSequenceStorage"><see cref="IEventSequenceStorage"/> to use.</param>
public class EventStoreNamespaceStorageForSpecifications(IEventSequenceStorage eventSequenceStorage) : IEventStoreNamespaceStorage
{
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
    public IEventSequenceStorage GetEventSequence(EventSequenceId eventSequenceId) => eventSequenceStorage;
}
