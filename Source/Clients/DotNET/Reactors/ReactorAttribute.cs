// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Attribute used to adorn classes to tell Cratis that the class is an Reactor.
/// </summary>
/// <param name="id">Optional <see cref="Id"/> represented as string, if not used it will default to the fully qualified type name.</param>
/// <param name="eventSequence">Optional the name of the event sequence to observe. When not specified, the event sequence is inferred from the event types handled by the reactor.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class ReactorAttribute(string id = "", string? eventSequence = default) : Attribute
{
    /// <summary>
    /// Gets the unique identifier for an Reactor.
    /// </summary>
    public ReactorId Id { get; } = id;

    /// <summary>
    /// Gets the explicit event sequence identifier, or <see langword="null"/> if not specified.
    /// </summary>
    /// <remarks>
    /// When <see langword="null"/>, the event sequence is inferred from the event types handled by the reactor.
    /// If all handled event types originate from the same event store (via <see cref="Events.EventStoreAttribute"/>),
    /// the reactor will automatically subscribe to the corresponding inbox event sequence.
    /// </remarks>
    public EventSequenceId? EventSequenceId { get; } = eventSequence is null ? null : new EventSequenceId(eventSequence);
}
