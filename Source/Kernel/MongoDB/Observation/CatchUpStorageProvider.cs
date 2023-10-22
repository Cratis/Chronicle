// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Grains.Observation;
using Aksio.Cratis.Observation;
using Aksio.DependencyInversion;
using Aksio.Strings;
using MongoDB.Driver;
using Orleans.Runtime;

namespace Aksio.Cratis.Kernel.MongoDB.Observation;

/// <summary>
/// Represents a <see cref="ObserverStorageProvider"/> for handling catch-up observer state storage.
/// </summary>
public class CatchUpStorageProvider : ObserverStorageProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CatchUpStorageProvider"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="eventStoreDatabaseProvider">Provider for <see cref="IEventStoreDatabase"/>.</param>
    /// <param name="eventSequenceStorageProvider">Provider for <see cref="IEventSequenceStorage"/>.</param>
    public CatchUpStorageProvider(
        IExecutionContextManager executionContextManager,
        ProviderFor<IEventStoreDatabase> eventStoreDatabaseProvider,
        ProviderFor<IEventSequenceStorage> eventSequenceStorageProvider)
        : base(executionContextManager, eventStoreDatabaseProvider, eventSequenceStorageProvider)
    {
    }

    /// <inheritdoc/>
    public override async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<ObserverState>)!;
        var observerId = grainId.GetGuidKey(out var observerKeyAsString);
        var observerKey = ObserverKey.Parse(observerKeyAsString!);
        var key = GetKeyFrom(observerKey, observerId);

        ExecutionContextManager.Establish(observerKey.TenantId, CorrelationId.New(), observerKey.MicroserviceId);

        var state = actualGrainState.State;

        // Note: The reason we're not using the ObserverState directly is for memory and performance reasons
        // it is faster to just store the next event sequence number directly in the document and less
        // memory footprint than creating an update statement based on the state object.
        var update = Builders<ObserverState>.Update
            .Set(nameof(ObserverState.NextEventSequenceNumber).ToCamelCase(), state.NextEventSequenceNumber)
            .Set(nameof(ObserverState.LastHandled).ToCamelCase(), state.LastHandled);

        await Collection.UpdateOneAsync(
            _ => _.Id == key,
            update,
            new UpdateOptions { IsUpsert = true }).ConfigureAwait(false);
    }
}
