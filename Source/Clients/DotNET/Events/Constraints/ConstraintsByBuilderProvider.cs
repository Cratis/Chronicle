// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Registrations;
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
    /// <inheritdoc/>
    public IImmutableList<IConstraintDefinition> Provide()
    {
        var definitions = ImmutableList.CreateBuilder<IConstraintDefinition>();

        var catalog = ExplicitArtifactCatalog.From(clientArtifactsProvider);
        foreach (var constraintType in clientArtifactsProvider.ConstraintTypes)
        {
            if (catalog?.IsExplicit(constraintType, ExplicitArtifactKind.Constraint) == true)
            {
                var activation = artifactActivator.ActivateNonDisposable(constraintType);
                if (activation.TryGetException(out var failure)) throw failure;
                var explicitBuilder = new ConstraintBuilder(eventTypes, namingPolicy, constraintType);
                ExplicitConstraintDefiner.Define(constraintType, activation.AsT0, explicitBuilder);
                definitions.AddRange(explicitBuilder.Build());
                continue;
            }

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

        return definitions.ToImmutable();
    }
}
