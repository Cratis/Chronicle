// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Attribute to specify which event sequence a type uses as its event source.
/// </summary>
/// <remarks>
/// <para>
/// Apply this attribute to a Reactor, Reducer, or model-bound projection read model type to override
/// the event sequence that the observer reads from. When set, auto-inbox routing and auto-subscription
/// are both suppressed — the explicit value is always honored.
/// </para>
/// <para>
/// The attribute targets any class, but is meaningful only on observer and read model types.
/// Use <see cref="EventLogAttribute"/> as a convenience alternative when pinning to the default event log.
/// </para>
/// <para>
/// Initializes a new instance of the <see cref="EventSequenceAttribute"/> class.
/// </para>
/// </remarks>
/// <param name="sequence">String representation of an <see cref="EventSequenceId"/>.</param>
[AttributeUsage(AttributeTargets.Class)]
#pragma warning disable CA1813 // Not sealed to allow EventLogAttribute to inherit from it
public class EventSequenceAttribute(string sequence) : Attribute
#pragma warning restore CA1813
{
    /// <summary>
    /// Gets the <see cref="EventSequenceId"/> to use.
    /// </summary>
    public EventSequenceId Sequence { get; } = sequence;
}
