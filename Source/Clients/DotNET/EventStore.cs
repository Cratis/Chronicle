// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Contracts.EventSequences;

namespace Aksio.Cratis;

/// <summary>
/// Represents an implementation of <see cref="IEventStore"/>.
/// </summary>
public class EventStore : IEventStore
{
    readonly IEventSequences _eventSequences;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStore"/> class.
    /// </summary>
    /// <param name="name">Name of the event store.</param>
    /// <param name="tenantId">Tenant identifier for the event store.</param>
    /// <param name="eventSequences"><see cref="IEventSequences"/> to use.</param>
    public EventStore(
        EventStoreName name,
        TenantId tenantId,
        IEventSequences eventSequences)
    {
        _eventSequences = eventSequences;
        EventLog = GetEventSequence(EventSequenceId.Log);
    }

    /// <inheritdoc/>
    public IEventLog EventLog { get; }

    /// <inheritdoc/>
    public IEventSequence GetEventSequence(EventSequenceId id) => throw new NotImplementedException();
}
