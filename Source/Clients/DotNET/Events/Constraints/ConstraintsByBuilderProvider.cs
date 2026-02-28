// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Serialization;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Represents a provider that can provide constraints that are built using the <see cref="IConstraintBuilder"/>.
/// </summary>
/// <param name="clientArtifactsProvider"><see cref="IClientArtifactsProvider"/> for providing client artifacts.</param>
/// <param name="eventTypes"><see cref="IEventTypes"/> for providing event types.</param>
/// <param name="namingPolicy">The <see cref="INamingPolicy"/> to use for converting names during serialization.</param>
/// <param name="artifactActivator">The <see cref="IClientArtifactsActivator"/> for activating artifacts.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
public class ConstraintsByBuilderProvider(
    IClientArtifactsProvider clientArtifactsProvider,
    IEventTypes eventTypes,
    INamingPolicy namingPolicy,
    IClientArtifactsActivator artifactActivator,
    ILogger<ConstraintsByBuilderProvider> logger) : ICanProvideConstraints
{
    readonly object _sync = new();
    IImmutableList<IConstraintDefinition>? _cachedDefinitions;

    /// <inheritdoc/>
    public IImmutableList<IConstraintDefinition> Provide()
    {
        if (_cachedDefinitions is not null)
        {
            return _cachedDefinitions;
        }

        lock (_sync)
        {
            if (_cachedDefinitions is not null)
            {
                return _cachedDefinitions;
            }

            var definitions = ImmutableList.CreateBuilder<IConstraintDefinition>();

            foreach (var constraintType in clientArtifactsProvider.ConstraintTypes)
            {
                var activatedArtifactResult = artifactActivator.ActivateNonDisposable<IConstraint>(constraintType);
                if (activatedArtifactResult.TryGetException(out var exception))
                {
                    logger.FailedToActivateConstraint(constraintType, exception);
                    continue;
                }

                var constraint = activatedArtifactResult.AsT0;
                var builder = new ConstraintBuilder(eventTypes, namingPolicy, constraintType);
                constraint.Define(builder);
                definitions.AddRange(builder.Build());
            }

            _cachedDefinitions = definitions.ToImmutable();
            return _cachedDefinitions;
        }
    }
}
