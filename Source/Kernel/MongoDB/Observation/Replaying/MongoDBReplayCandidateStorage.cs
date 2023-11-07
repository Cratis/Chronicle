// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Observation.Replaying;
using Aksio.Cratis.Kernel.Persistence.Observation.Replaying;
using Aksio.DependencyInversion;
using MongoDB.Driver;

namespace Aksio.Cratis.Kernel.MongoDB.Observation;

/// <summary>
/// Represents an implementation of <see cref="IReplayCandidatesStorage"/> for MongoDB.
/// </summary>
public class MongoDBReplayCandidateStorage : IReplayCandidatesStorage
{
    readonly IExecutionContextManager _executionContextManager;
    readonly ProviderFor<IEventStoreDatabase> _eventStoreDatabaseProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDBReplayCandidateStorage"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="eventStoreDatabaseProvider">Provider for <see cref="IEventStoreDatabase"/>.</param>
    public MongoDBReplayCandidateStorage(
        IExecutionContextManager executionContextManager,
        ProviderFor<IEventStoreDatabase> eventStoreDatabaseProvider)
    {
        _executionContextManager = executionContextManager;
        _eventStoreDatabaseProvider = eventStoreDatabaseProvider;
    }

    IMongoCollection<ReplayCandidate> Collection => _eventStoreDatabaseProvider().GetCollection<ReplayCandidate>(WellKnownCollectionNames.ReplayCandidates);

    /// <inheritdoc/>
    public async Task Add(ReplayCandidate replayCandidate)
    {
        _executionContextManager.Establish(
            replayCandidate.ObserverKey.TenantId,
            _executionContextManager.Current.CorrelationId,
            replayCandidate.ObserverKey.MicroserviceId);

        await Collection.InsertOneAsync(replayCandidate).ConfigureAwait(false);
    }
}
