// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Services.Events.Constraints;

/// <summary>
/// Represents converter methods between <see cref="Contracts.Events.Constraints.Constraint"/> and <see cref="IConstraintDefinition"/>.
/// </summary>
internal static class ConstraintConverters
{
    /// <summary>
    /// Convert <see cref="Contracts.Events.Constraints.Constraint"/> to <see cref="IConstraintDefinition"/>.
    /// </summary>
    /// <param name="constraint"><see cref="Contracts.Events.Constraints.Constraint"/> to convert from.</param>
    /// <returns>Collection of <see cref="IConstraintDefinition"/>.</returns>
    public static IConstraintDefinition ToChronicle(this Contracts.Events.Constraints.Constraint constraint)
    {
        var scope = constraint.Scope?.ToChronicle();
        var eventSequences = constraint.ToEventSequences();

        return constraint.Type switch
        {
            Contracts.Events.Constraints.ConstraintType.Unique =>
                new UniqueConstraintDefinition(
                    constraint.Name,
                    constraint.Definition.Value0!.EventDefinitions.Select(e => e.ToChronicle()),
                    constraint.ToRemovedWith(),
                    constraint.Definition.Value0!.IgnoreCasing,
                    scope)
                {
                    EventSequences = eventSequences,
                    Mode = (UniqueConstraintMode)constraint.Definition.Value0!.Mode,
                    RemovalEventDefinitions = constraint.Definition.Value0!.RemovalEventDefinitions.Select(_ => _.ToChronicle()).ToArray()
                },

            Contracts.Events.Constraints.ConstraintType.UniqueEventType =>
                new UniqueEventTypeConstraintDefinition(
                    constraint.Name,
                    constraint.Definition.Value1!.EventTypeIds.Select(_ => (EventTypeId)_).ToArray(),
                    constraint.ToRemovedWith(),
                    scope)
                {
                    EventSequences = eventSequences
                },

            _ => null!
        };
    }

    /// <summary>
    /// Convert a contract <see cref="Contracts.Events.Constraints.ConstraintScope"/> to a Chronicle <see cref="ConstraintScope"/>.
    /// </summary>
    /// <param name="scope"><see cref="Contracts.Events.Constraints.ConstraintScope"/> to convert.</param>
    /// <returns>Converted <see cref="ConstraintScope"/>.</returns>
    public static ConstraintScope? ToChronicle(this Contracts.Events.Constraints.ConstraintScope scope)
    {
        if (scope.EventSourceType is null && scope.EventStreamType is null && scope.EventStreamId is null)
        {
            return null;
        }

        return new ConstraintScope(
            scope.EventSourceType is not null ? (EventSourceType)scope.EventSourceType : null,
            scope.EventStreamType is not null ? (EventStreamType)scope.EventStreamType : null,
            scope.EventStreamId is not null ? (EventStreamId)scope.EventStreamId : null);
    }

    /// <summary>
    /// Convert the removal event types a contract <see cref="Contracts.Events.Constraints.Constraint"/> carries.
    /// </summary>
    /// <param name="constraint"><see cref="Contracts.Events.Constraints.Constraint"/> to read from.</param>
    /// <returns>The <see cref="EventTypeId"/> values of the events that release the constraint.</returns>
    /// <remarks>
    /// A client older than the plural form sends its one removal event on this same field, and arrives here as a
    /// one-element collection rather than as nothing: the field kept its number, and one length-delimited value is
    /// indistinguishable on the wire from a repeated field holding one. That is what makes the kernel safe to
    /// upgrade ahead of its clients, which is the order the release notes ask for.
    /// </remarks>
    static EventTypeId[] ToRemovedWith(this Contracts.Events.Constraints.Constraint constraint) =>
        constraint.RemovedWith?.Select(_ => (EventTypeId)_).ToArray() ?? [];

    /// <summary>
    /// Convert the event sequences a contract <see cref="Contracts.Events.Constraints.Constraint"/> applies to.
    /// </summary>
    /// <param name="constraint"><see cref="Contracts.Events.Constraints.Constraint"/> to read from.</param>
    /// <returns>The <see cref="EventSequenceId"/> values of the event sequences the constraint applies to, empty for all.</returns>
    /// <remarks>
    /// A client that predates the field sends nothing, which arrives as an empty collection and keeps the constraint
    /// applying to every event sequence.
    /// </remarks>
    static EventSequenceId[] ToEventSequences(this Contracts.Events.Constraints.Constraint constraint) =>
        constraint.EventSequences?
            .Where(_ => !string.IsNullOrWhiteSpace(_))
            .Distinct()
            .Select(_ => (EventSequenceId)_)
            .ToArray() ?? [];
}
