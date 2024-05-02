// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.EventSequences;
using Cratis.Kernel.Storage;
using Cratis.Kernel.Storage.Changes;
using Cratis.Kernel.Storage.EventSequences;
using Cratis.Kernel.Storage.Jobs;
using Cratis.Kernel.Storage.Keys;
using Cratis.Kernel.Storage.Observation;
using Cratis.Kernel.Storage.Recommendations;

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
