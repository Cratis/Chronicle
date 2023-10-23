// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Grains.Observation;
using Aksio.Cratis.Observation;
using Aksio.DependencyInversion;
using Aksio.Strings;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Orleans.Runtime;

namespace Aksio.Cratis.Kernel.MongoDB.Observation;

/// <summary>
/// Represents a <see cref="ObserverStorageProvider"/> for handling replay observer state storage.
/// </summary>
public class ReplayStorageProvider : ObserverStorageProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReplayStorageProvider"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="eventStoreDatabaseProvider">Provider for <see cref="IEventStoreDatabase"/>.</param>
    /// <param name="eventSequenceStorageProvider">Provider for <see cref="IEventSequenceStorage"/>.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public ReplayStorageProvider(
        IExecutionContextManager executionContextManager,
        ProviderFor<IEventStoreDatabase> eventStoreDatabaseProvider,
        ProviderFor<IEventSequenceStorage> eventSequenceStorageProvider,
        ILogger<ObserverStorageProvider> logger)
        : base(executionContextManager, eventStoreDatabaseProvider, eventSequenceStorageProvider, logger)
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
        var update = Builders<ObserverState>.Update.Set(
            nameof(ObserverState.NextEventSequenceNumber).ToCamelCase(),
            state.NextEventSequenceNumber);

        await Collection.UpdateOneAsync(
            _ => _.Id == key,
            update,
            new UpdateOptions { IsUpsert = true }).ConfigureAwait(false);
    }
}
