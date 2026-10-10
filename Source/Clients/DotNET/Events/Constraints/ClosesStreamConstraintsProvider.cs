// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Properties;
using Cratis.Serialization;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Provides closing constraints discovered from event types carrying <see cref="ClosesStreamAttribute"/>.
/// </summary>
/// <param name="clientArtifactsProvider">The client artifacts.</param>
/// <param name="eventTypes">The event types and schemas.</param>
/// <param name="namingPolicy">The payload naming policy.</param>
public class ClosesStreamConstraintsProvider(IClientArtifactsProvider clientArtifactsProvider, IEventTypes eventTypes, INamingPolicy namingPolicy) : ICanProvideConstraints
{
    /// <inheritdoc/>
    /// <exception cref="PropertyDoesNotExistOnEventType">A closing or reopening type lacks the declared property.</exception>
    /// <exception cref="ConflictingClosesStreamDeclarations">Same-named declarations disagree on their scope.</exception>
    /// <exception cref="MissingNameForClosesStreamConstraint">A declaration uses the reserved manual owner name.</exception>
    public IImmutableList<IConstraintDefinition> Provide() => ClosesStreamDeclarations.Merge(
        clientArtifactsProvider.ClosesStreamEventTypes.Select(type =>
        {
            var declaration = type.GetClosesStream()!;
            var property = declaration.EventStreamIdFrom is null ? null : namingPolicy.GetPropertyName(new PropertyPath(declaration.EventStreamIdFrom));
            ClosesStreamDeclarations.ValidateProperty(eventTypes, new[] { type }.Concat(declaration.ReopenedBy), property);
            var dimensions = declaration.Dimensions;
            if (property is not null) dimensions |= ClosedStreamDimensions.EventStreamId;

            return new ClosesStreamConstraintDefinition(
                declaration.Name ?? type.Name,
                _ => ConstraintViolationMessage.NotDefined,
                [eventTypes.GetEventTypeFor(type).Id],
                dimensions,
                declaration.ReopenedBy.Select(reopening => eventTypes.GetEventTypeFor(reopening).Id).Distinct().ToArray(),
                property)
            {
                EventSequences = declaration.EventSequences.Where(sequence => !string.IsNullOrWhiteSpace(sequence)).Distinct().Select(sequence => new EventSequenceId(sequence)).ToArray()
            };
        })).ToImmutableList();
}
