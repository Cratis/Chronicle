// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store.Branching;
using Aksio.Cratis.Events.Store.Grains.Branching;
using Aksio.Cratis.Execution;
using Orleans;

namespace Aksio.Cratis.Events;

/// <summary>
/// Represents an implementation of <see cref="IEventLog"/>.
/// </summary>
public class EventLog : IEventLog
{
    readonly IEventTypes _eventTypes;
    readonly IEventSerializer _serializer;
    readonly Store.Grains.IEventSequence _eventLog;
    readonly IClusterClient _clusterClient;
    readonly IExecutionContextManager _executionContextManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventLog"/> class.
    /// </summary>
    /// <param name="eventTypes"><see cref="IEventTypes"/> for resolving the types of events.</param>
    /// <param name="serializer"><see cref="IEventSerializer"/> for serializing events.</param>
    /// <param name="eventLog">The actual <see cref="Store.Grains.IEventSequence"/>.</param>
    /// <param name="clusterClient"><see cref="IClusterClient"/> for working with Orleans.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> to work with execution context.</param>
    public EventLog(
        IEventTypes eventTypes,
        IEventSerializer serializer,
        Store.Grains.IEventSequence eventLog,
        IClusterClient clusterClient,
        IExecutionContextManager executionContextManager)
    {
        _eventTypes = eventTypes;
        _serializer = serializer;
        _eventLog = eventLog;
        _clusterClient = clusterClient;
        _executionContextManager = executionContextManager;
    }

    /// <inheritdoc/>
    public async Task Append(EventSourceId eventSourceId, object @event, DateTimeOffset? validFrom = default)
    {
        var eventType = _eventTypes.GetEventTypeFor(@event.GetType());
        var eventAsJson = await _serializer.Serialize(@event!);
        await _eventLog.Append(eventSourceId, eventType, eventAsJson, validFrom);
    }

    /// <inheritdoc/>
    public async Task<IBranch> Branch(
        BranchTypeId? branchTypeId = default,
        EventSequenceNumber? branchFrom = default,
        IDictionary<string, string>? tags = default)
    {
        branchTypeId ??= BranchTypeId.NotSpecified;

        var branches = _clusterClient.GetGrain<IBranches>(Guid.Empty);
        var branchId = await branches.Checkout(branchTypeId, tags);
        var executionContext = _executionContextManager.Current;
        var branchKey = new BranchKey(executionContext.TenantId, executionContext.MicroserviceId);
        var actualBranch = _clusterClient.GetGrain<Store.Grains.Branching.IBranch>(branchId, branchKey);
        var eventSequence = _clusterClient.GetGrain<Store.Grains.IEventSequence>(
            branchId,
            keyExtension: executionContext.ToMicroserviceAndTenant());

        return new Branch(branchTypeId, branchId, _serializer, _eventTypes, eventSequence, actualBranch);
    }

    /// <inheritdoc/>
    public Task<IBranch> GetBranch(BranchId branchId)
    {
        return Task.FromResult<IBranch>(null!);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<BranchDescriptor>> GetBranchesFor(BranchTypeId branchTypeId)
    {
        var branches = _clusterClient.GetGrain<IBranches>(Guid.Empty);
        return branches.GetFor(branchTypeId);
    }
}
