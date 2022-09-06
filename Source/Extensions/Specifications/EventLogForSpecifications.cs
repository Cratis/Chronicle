// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Events.Store.Branching;

namespace Aksio.Cratis.Specifications;

/// <summary>
/// Represents an implementation of <see cref="IEventLog"/> for specifications.
/// </summary>
public class EventLogForSpecifications : IEventLog
{
    readonly EventSequenceForSpecifications _sequence = new();

    /// <summary>
    /// Gets the appended events.
    /// </summary>
    public IEnumerable<AppendedEventForSpecifications> AppendedEvents => _sequence.AppendedEvents;

    /// <inheritdoc/>
    public Task Append(EventSourceId eventSourceId, object @event, DateTimeOffset? validFrom = null) => _sequence.Append(eventSourceId, @event);

    /// <inheritdoc/>
    public Task<IBranch> Branch(BranchTypeId? branchTypeId = null, EventSequenceNumber? from = null, IDictionary<string, string>? tags = null) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task<IBranch> GetBranch(BranchId branchId) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task<IEnumerable<BranchDescriptor>> GetBranchesFor(BranchTypeId branchTypeId) => throw new NotImplementedException();
}
