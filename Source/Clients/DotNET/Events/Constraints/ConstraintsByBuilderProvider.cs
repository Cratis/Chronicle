// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Serialization;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Represents a provider that can provide constraints that are built using the <see cref="IConstraintBuilder"/>.
/// </summary>
/// <param name="clientArtifactsProvider"><see cref="IClientArtifactsProvider"/> for providing client artifacts.</param>
/// <param name="eventTypes"><see cref="IEventTypes"/> for providing event types.</param>
/// <param name="namingPolicy">The <see cref="INamingPolicy"/> to use for converting names during serialization.</param>
/// <param name="artifactActivator">The <see cref="IArtifactActivator"/> for activating artifacts.</param>
public class ConstraintsByBuilderProvider(
    IClientArtifactsProvider clientArtifactsProvider,
    IEventTypes eventTypes,
    INamingPolicy namingPolicy,
    IArtifactActivator artifactActivator) : ICanProvideConstraints
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
                var activatedArtifact = artifactActivator.CreateInstance(constraintType);
                try
                {
                    if (activatedArtifact.Instance is not IConstraint constraint)
                    {
                        throw new InvalidOperationException($"Type '{constraintType.FullName}' does not implement IConstraint");
                    }

                    var builder = new ConstraintBuilder(eventTypes, namingPolicy, constraintType);
                    constraint.Define(builder);
                    definitions.AddRange(builder.Build());
                }
                finally
                {
                    activatedArtifact.DisposeAsync().AsTask().GetAwaiter().GetResult();
                }
            }

            _cachedDefinitions = definitions.ToImmutable();
            return _cachedDefinitions;
        }
    }
}
