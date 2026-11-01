// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Defines a command that writes to an event sequence and may therefore create it.
/// </summary>
internal interface IEventSequenceWriteCommand
{
    /// <summary>
    /// Gets the event store name.
    /// </summary>
    EventStoreName EventStore { get; }

    /// <summary>
    /// Gets the namespace name.
    /// </summary>
    EventStoreNamespaceName Namespace { get; }

    /// <summary>
    /// Gets the event sequence identifier.
    /// </summary>
    Concepts.EventSequences.EventSequenceId EventSequenceId { get; }
}
