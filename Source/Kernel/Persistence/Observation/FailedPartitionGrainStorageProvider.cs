// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Observation;
using Aksio.Cratis.Observation;
using Aksio.DependencyInversion;
using Orleans.Runtime;
using Orleans.Storage;

namespace Aksio.Cratis.Kernel.Persistence.Observation;

/// <summary>
/// Represents an implementation of <see cref="IGrainStorage"/> for handling <see cref="FailedPartition" /> storage.
/// </summary>
public class FailedPartitionGrainStorageProvider : IGrainStorage
{
    readonly ProviderFor<IFailedPartitionsStorage> _failedPartitionsStorageProvider;

    readonly IExecutionContextManager _executionContextManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="FailedPartitionGrainStorageProvider"/> class.
    /// </summary>
    /// <param name="failedPartitionsStorageProvider">Provider for <see cref="IFailedPartitionsStorage"/> for actual persistence.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    public FailedPartitionGrainStorageProvider(
        ProviderFor<IFailedPartitionsStorage> failedPartitionsStorageProvider,
        IExecutionContextManager executionContextManager)
    {
        _failedPartitionsStorageProvider = failedPartitionsStorageProvider;
        _executionContextManager = executionContextManager;
    }

    /// <inheritdoc/>
    public Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState) => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<FailedPartitions>)!;
        var observerId = grainId.GetGuidKey(out var observerKeyAsString);
        var observerKey = ObserverKey.Parse(observerKeyAsString!);

        _executionContextManager.Establish(observerKey.TenantId, CorrelationId.New(), observerKey.MicroserviceId);
        actualGrainState.State = await _failedPartitionsStorageProvider().GetFor(observerId);
    }

    /// <inheritdoc/>
    public virtual async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<FailedPartitions>)!;
        var observerId = grainId.GetGuidKey(out var observerKeyAsString);
        var observerKey = ObserverKey.Parse(observerKeyAsString!);

        _executionContextManager.Establish(observerKey.TenantId, CorrelationId.New(), observerKey.MicroserviceId);
        await _failedPartitionsStorageProvider().Save(observerId, actualGrainState.State);
    }
}
