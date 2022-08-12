// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store.Branching;
using Aksio.Cratis.Execution;
using Orleans;
using Orleans.Providers;

namespace Aksio.Cratis.Events.Store.Grains.Branching;

/// <summary>
/// Represents an implementation of <see cref="IBranches"/>.
/// </summary>
[StorageProvider(ProviderName = BranchesState.StorageProvider)]
public class Branches : Grain<BranchesState>, IBranches
{
    readonly IExecutionContextManager _executionContextManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="Branches"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    public Branches(IExecutionContextManager executionContextManager)
    {
        _executionContextManager = executionContextManager;
    }

    /// <inheritdoc/>
    public override Task OnActivateAsync()
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<BranchId> Checkout(BranchTypeId branchTypeId, EventSequenceNumber? from = null, IDictionary<string, string>? labels = null)
    {
        var eventLog = GrainFactory.GetGrain<IEventSequence>(
            EventSequenceId.Log,
            keyExtension: _executionContextManager.Current.ToMicroserviceAndTenant());

        from ??= await eventLog.GetTailSequenceNumber();
        labels ??= new Dictionary<string, string>();
        var branchId = BranchId.New();
        State.Branches.Add(new(branchId, branchTypeId, from, DateTimeOffset.UtcNow, labels));
        await WriteStateAsync();

        return branchId;
    }

    /// <inheritdoc/>
    public Task Conclude(BranchId branchId) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task<IEnumerable<BranchDescriptor>> GetFor(BranchTypeId branchTypeId) => throw new NotImplementedException();
}
