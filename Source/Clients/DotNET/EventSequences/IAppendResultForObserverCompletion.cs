// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Defines the context required to wait for observer completion after an append operation.
/// </summary>
public interface IAppendResultForObserverCompletion
{
    /// <summary>
    /// Gets the event store where the append operation happened.
    /// </summary>
    EventStoreName EventStore { get; }

    /// <summary>
    /// Gets the event store namespace where the append operation happened.
    /// </summary>
    EventStoreNamespaceName EventStoreNamespace { get; }

    /// <summary>
    /// Gets the event sequence that the append operation targeted.
    /// </summary>
    EventSequenceId EventSequenceId { get; }

    /// <summary>
    /// Gets the last event sequence number represented by the append operation.
    /// </summary>
    EventSequenceNumber TailSequenceNumber { get; }

    /// <summary>
    /// Gets the observer service used for waiting for completion.
    /// </summary>
    internal Contracts.Observation.IObservers? Observers =>
        this switch
        {
            AppendResult appendResult => appendResult.Observers,
            AppendManyResult appendManyResult => appendManyResult.Observers,
            _ => null
        };
}
