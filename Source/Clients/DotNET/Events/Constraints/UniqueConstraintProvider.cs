// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Serialization;
using Cratis.Reflection;

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
            var builder = new ConstraintBuilder(eventTypes, namingPolicy);
            builder.Unique(unique =>
            {
                unique.WithName(constraint.Key);
                var propertyNames = constraint.Select(_ => _.Property.Name).ToArray();

                foreach (var constrainedProperty in constraint)
                {
                    unique.On(eventTypes.GetEventTypeFor(constrainedProperty.EventType), [constrainedProperty.Property.Name]);
                }
            });
            constraints.AddRange(builder.Build());
        }
        return constraints.ToImmutableList();
    }
}
