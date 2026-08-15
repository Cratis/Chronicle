// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Reflection;
using Cratis.Serialization;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Represents a provider that can provide constraints for unique properties based on properties on an event type adorned with <see cref="UniqueAttribute"/>.
/// </summary>
/// <param name="clientArtifactsProvider"><see cref="IClientArtifactsProvider"/> for providing client artifacts.</param>
/// <param name="eventTypes"><see cref="IEventTypes"/> for providing event types.</param>
/// <param name="namingPolicy">The <see cref="INamingPolicy"/> to use for converting names during serialization.</param>
public class UniqueConstraintProvider(
    IClientArtifactsProvider clientArtifactsProvider,
    IEventTypes eventTypes,
    INamingPolicy namingPolicy) : ICanProvideConstraints
{
    /// <inheritdoc/>
    public IImmutableList<IConstraintDefinition> Provide()
    {
        var uniqueConstraints = clientArtifactsProvider.UniqueConstraints
            .SelectMany(eventType =>
                eventType
                    .GetProperties()
                    .Where(property => property.HasAttribute<UniqueAttribute>())
                    .Select(property => new
                    {
                        ConstraintName = property.GetConstraintName(),
                        EventType = eventType,
                        Property = property
                    }))
            .GroupBy(property => property.ConstraintName);

        var constraints = new List<IConstraintDefinition>();
        foreach (var constraint in uniqueConstraints)
        {
            // Every event type declaring [RemoveConstraint] for this name releases it, not just the first one
            // found. A lifecycle can end in more than one way, and each of those facts is a release.
            var removalEventTypes = clientArtifactsProvider.RemoveConstraintEventTypes
                .Where(t => t.GetRemoveConstraints().Any(a => constraint.Key == (ConstraintName)a.ConstraintName))
                .ToArray();

            var builder = new ConstraintBuilder(eventTypes, namingPolicy);
            builder.Unique(unique =>
            {
                unique.WithName(constraint.Key);

                // The message the author wrote, where the name is already read. It used to be dropped here and
                // nowhere else - the class-level provider reads the same argument off the same attribute - so a
                // property-level [Unique(message:)] registered, enforced, rejected the append correctly and
                // surfaced the kernel's default text instead. Having watched the rejection happen, an author has
                // every reason to look for the loss in their own presentation layer.
                //
                // Several properties can share one constraint name, and one constraint carries one message, so
                // the first supplied wins - the same answer the fluent form gives when it merges same-named
                // definitions and keeps the first callback. A constraint where nobody supplied one keeps the
                // empty default, which is what it had before.
                var message = constraint
                    .Select(_ => _.Property.GetConstraintMessage())
                    .FirstOrDefault(_ => _ != ConstraintViolationMessage.NotDefined);

                if (message is not null)
                {
                    unique.WithMessage(message);
                }

                var propertyNames = constraint.Select(_ => _.Property.Name).ToArray();

                foreach (var constrainedProperty in constraint)
                {
                    unique.On(eventTypes.GetEventTypeFor(constrainedProperty.EventType), [constrainedProperty.Property.Name]);
                }

                foreach (var removalEventType in removalEventTypes)
                {
                    unique.RemovedWith(eventTypes.GetEventTypeFor(removalEventType));
                }
            });
            constraints.AddRange(builder.Build());
        }
        return constraints.ToImmutableList();
    }
}
