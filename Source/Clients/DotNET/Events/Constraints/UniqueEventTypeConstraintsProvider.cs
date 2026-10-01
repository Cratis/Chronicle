// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Represents a provider that can provide constraints for unique event types based on event types adorned with <see cref="UniqueAttribute"/> .
/// </summary>
/// <remarks>
/// Event types declaring the same constraint name are one constraint, the way <c language="csharp">Unique&lt;A&gt;(name: x)</c> and
/// <c language="csharp">Unique&lt;B&gt;(name: x)</c> are on a <see cref="IConstraintBuilder"/>: it allows at most one event from
/// the declaring event types per event source. Providing them as separate definitions would make each check only its own event
/// type, and leave the name resolving to more than one definition.
/// </remarks>
/// <param name="clientArtifactsProvider"><see cref="IClientArtifactsProvider"/> for providing client artifacts.</param>
/// <param name="eventTypes"><see cref="IEventTypes"/> for providing event types.</param>
public class UniqueEventTypeConstraintsProvider(IClientArtifactsProvider clientArtifactsProvider, IEventTypes eventTypes) : ICanProvideConstraints
{
    /// <inheritdoc/>
    public IImmutableList<IConstraintDefinition> Provide() =>
        clientArtifactsProvider.UniqueEventTypeConstraints
            .GroupBy(eventType => eventType.GetConstraintName())
            .Select<IGrouping<ConstraintName, Type>, IConstraintDefinition>(declarations =>
            {
                var constraintName = declarations.Key;

                // Every event type declaring [RemoveConstraint] for this name releases it, not just the first one
                // found. A lifecycle can end in more than one way, and each of those facts is a release.
                var removedWith = clientArtifactsProvider.RemoveConstraintEventTypes
                    .Where(t => t.GetRemoveConstraints().Any(a => constraintName == (ConstraintName)a.ConstraintName))
                    .Select(t => eventTypes.GetEventTypeFor(t).Id)
                    .Distinct()
                    .ToArray();

                var declared = declarations
                    .Select(eventType => (EventTypeId: eventTypes.GetEventTypeFor(eventType).Id, Message: eventType.GetConstraintMessage()))
                    .ToArray();

                return new UniqueEventTypeConstraintDefinition(
                    constraintName,
                    violation => MessageFor(declared, violation),
                    [.. declared.Select(_ => _.EventTypeId).Distinct()],
                    removedWith)
                {
                    EventSequences = ConstraintEventSequences.Combine(declarations.Select(eventType => eventType.GetConstraintEventSequences()))
                };
            }).ToImmutableList();

    static ConstraintViolationMessage MessageFor(
        (EventTypeId EventTypeId, ConstraintViolationMessage Message)[] declared,
        ConstraintViolation violation)
    {
        // A declaration states the message for its own event type, so the violating event type picks it. Falling
        // back to the first message stated at all keeps a violation reported against another event type of the
        // constraint from losing its message.
        if (declared.Length == 1)
        {
            return declared[0].Message;
        }

        var stated = declared.Where(_ => !string.IsNullOrEmpty(_.Message.Value)).ToArray();
        if (stated.Length == 0)
        {
            return ConstraintViolationMessage.NotDefined;
        }

        var own = stated.Where(_ => _.EventTypeId == violation.EventTypeId).ToArray();
        return own.Length > 0 ? own[0].Message : stated[0].Message;
    }
}
