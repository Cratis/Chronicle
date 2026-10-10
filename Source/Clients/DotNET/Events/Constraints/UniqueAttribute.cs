// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Attribute to adorn types and properties on event types to indicate uniqueness.
/// </summary>
/// <remarks>
/// Property constraints default to one value per event source, unique across event sources. Claiming a new value
/// releases the previous one. Set <see cref="Mode"/> to <see cref="UniqueConstraintMode.PerValue"/> to retain every
/// value until a removal event releases it; reclaiming a value by the same source is accepted in both modes.
/// Class-level uniqueness remains one event of that type per event source, regardless of this setting.
/// <para>
/// An attribute argument is a compile-time constant, so a message written here is fixed in one language. A
/// consumer that localizes wants the fluent <c language="csharp">IConstraint</c> form instead, whose
/// <c language="csharp">WithMessage(ConstraintViolationMessageProvider)</c> resolves per access and so can follow the current
/// culture.
/// </para>
/// </remarks>
/// <param name="name">Optional name of the constraint to use.</param>
/// <param name="message">Optional message to use when the unique constraint is violated.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false)]
public sealed class UniqueAttribute(string? name = default, string? message = default) : Attribute
{
    /// <summary>
    /// Gets the name of the constraint.
    /// </summary>
    public string? Name { get; } = name;

    /// <summary>
    /// Gets the message to use when the unique constraint is violated.
    /// </summary>
    public string? Message { get; } = message;

    /// <summary>
    /// Gets or sets how a property constraint retains values.
    /// </summary>
    public UniqueConstraintMode Mode { get; set; } = UniqueConstraintMode.PerEventSource;

    /// <summary>
    /// Gets or sets the identifiers of the event sequences the constraint applies to.
    /// </summary>
    /// <remarks>
    /// Empty means every event sequence, which is the default. The kernel neither validates nor indexes the constraint
    /// for an event sequence it does not apply to - declare the event log alone when the same event type is also
    /// forwarded to the outbox:
    /// <c language="csharp">[Unique(EventSequences = [EventSequenceId.LogId])]</c>.
    /// <para>
    /// When several properties share a constraint name, the event sequences declared on each of them are combined. A
    /// property that declares none applies the constraint to every event sequence, and that wins: the combined
    /// constraint then applies to every event sequence.
    /// </para>
    /// </remarks>
    public string[] EventSequences { get; set; } = [];
}
