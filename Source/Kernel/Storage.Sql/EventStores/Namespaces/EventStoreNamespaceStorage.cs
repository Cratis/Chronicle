// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Changes;
using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Identities;
using Cratis.Chronicle.Storage.Jobs;
using Cratis.Chronicle.Storage.Keys;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Chronicle.Storage.Recommendations;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;

/// <summary>
/// Represents an implementation of <see cref="IEventStoreNamespaceStorage"/> for SQL.
/// </summary>
/// <param name="eventStore">The name of the event store.</param>
/// <param name="database">The <see cref="IDatabase"/> to use for storage operations.</param>
public class EventStoreNamespaceStorage(EventStoreName eventStore, IDatabase database) : IEventStoreNamespaceStorage
{
    /// <inheritdoc/>
    public IChangesetStorage Changesets => throw new NotImplementedException();

    /// <inheritdoc/>
    public IIdentityStorage Identities => throw new NotImplementedException();

    /// <inheritdoc/>
    public IJobStorage Jobs => throw new NotImplementedException();

    /// <inheritdoc/>
    public IJobStepStorage JobSteps => throw new NotImplementedException();

    /// <inheritdoc/>
    public IObserverStateStorage Observers => throw new NotImplementedException();

    /// <inheritdoc/>
    public IFailedPartitionsStorage FailedPartitions => throw new NotImplementedException();

    /// <inheritdoc/>
    public IRecommendationStorage Recommendations => throw new NotImplementedException();

    /// <inheritdoc/>
    public IObserverKeyIndexes ObserverKeyIndexes => throw new NotImplementedException();

    /// <inheritdoc/>
    public IReplayContexts ReplayContexts => throw new NotImplementedException();

    /// <inheritdoc/>
    public ISinks Sinks => throw new NotImplementedException();

    /// <inheritdoc/>
    public IReplayedModelsStorage ReplayedModels => throw new NotImplementedException();

    /// <inheritdoc/>
    public IEventSequenceStorage GetEventSequence(EventSequenceId eventSequenceId) => throw new NotImplementedException();

    /// <inheritdoc/>
    public IUniqueConstraintsStorage GetUniqueConstraintsStorage(EventSequenceId eventSequenceId) => throw new NotImplementedException();

    /// <inheritdoc/>
    public IUniqueEventTypesConstraintsStorage GetUniqueEventTypesConstraints(EventSequenceId eventSequenceId) => throw new NotImplementedException();
}
