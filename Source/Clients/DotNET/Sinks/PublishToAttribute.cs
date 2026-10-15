// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sinks;

/// <summary>
/// Attribute to adorn an event type to select the event sequence a projection or reducer that targets it publishes to.
/// </summary>
/// <remarks>
/// Without the attribute the outbox is the destination. The event log is never a valid destination.
/// </remarks>
/// <param name="sequence">String representation of the destination <see cref="EventSequences.EventSequenceId"/>.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class PublishToAttribute(string sequence) : Attribute
{
    /// <summary>
    /// Gets the destination <see cref="EventSequences.EventSequenceId"/>.
    /// </summary>
    public EventSequences.EventSequenceId Sequence { get; } = sequence;
}
