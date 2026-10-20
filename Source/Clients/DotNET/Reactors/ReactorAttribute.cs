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
    /// Initializes a new instance of the <see cref="ReactorAttribute"/> class with an effect epoch.
    /// </summary>
    /// <param name="id">The <see cref="Id"/> represented as string, if empty it will default to the fully qualified type name.</param>
    /// <param name="effectEpoch">The <see cref="EffectEpoch"/> of the reactor.</param>
    public ReactorAttribute(string id, uint effectEpoch)
            : this(id)
    {
        EffectEpoch = effectEpoch;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReactorAttribute"/> class with an event sequence and an effect epoch.
    /// </summary>
    /// <param name="id">The <see cref="Id"/> represented as string, if empty it will default to the fully qualified type name.</param>
    /// <param name="eventSequence">The name of the event sequence to observe, or <see langword="null"/> to infer it from the handled event types.</param>
    /// <param name="effectEpoch">The <see cref="EffectEpoch"/> of the reactor.</param>
    public ReactorAttribute(string id, string? eventSequence, uint effectEpoch)
        : this(id, eventSequence)
    {
        EffectEpoch = effectEpoch;
    }

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

    /// <summary>
    /// Gets the effect epoch of the reactor. Defaults to 0.
    /// </summary>
    /// <remarks>
    /// The epoch is part of the idempotency key Chronicle derives for the events a reactor returns. Raising it starts
    /// a new key space, so a delivery whose output changed can be appended again under new keys. It does not change
    /// the reactor's id or the <see cref="ReactorDelivery.Id"/> of any delivery.
    /// </remarks>
    public uint EffectEpoch { get; private init; }
}
