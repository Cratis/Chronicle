// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Represents a provider that can provide constraints for unique event types based on event types adorned with <see cref="UniqueAttribute"/> .
/// </summary>
/// <param name="clientArtifactsProvider"><see cref="IClientArtifactsProvider"/> for providing client artifacts.</param>
/// <param name="eventTypes"><see cref="IEventTypes"/> for providing event types.</param>
public class UniqueEventTypeConstraintsProvider(IClientArtifactsProvider clientArtifactsProvider, IEventTypes eventTypes) : ICanProvideConstraints
{
    /// <inheritdoc/>
    public IImmutableList<IConstraintDefinition> Provide() =>
        clientArtifactsProvider.UniqueEventTypeConstraints
            .Select(eventType => new UniqueEventTypeConstraintDefinition(
                eventType.GetConstraintName(),
                et => eventType.GetConstraintMessage() ?? string.Empty,
                eventTypes.GetEventTypeFor(eventType)) as IConstraintDefinition).ToImmutableList();
}
