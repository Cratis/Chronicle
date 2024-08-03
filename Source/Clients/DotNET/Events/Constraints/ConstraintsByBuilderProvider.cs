// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Represents a provider that can provide constraints that are built using the <see cref="IConstraintBuilder"/>.
/// </summary>
/// <param name="clientArtifactsProvider"><see cref="IClientArtifactsProvider"/> for providing client artifacts.</param>
/// <param name="eventTypes"><see cref="IEventTypes"/> for providing event types.</param>
/// <param name="serviceProvider"><see cref="IServiceProvider"/> for providing services.</param>
public class ConstraintsByBuilderProvider(
    IClientArtifactsProvider clientArtifactsProvider,
    IEventTypes eventTypes,
    IServiceProvider serviceProvider) : ICanProvideConstraints
{
    /// <inheritdoc/>
    public IImmutableList<IConstraintDefinition> Provide() =>
        clientArtifactsProvider.ConstraintTypes
            .Select(type => (serviceProvider.GetRequiredService(type) as IConstraint)!)
            .SelectMany(constraint =>
            {
                var builder = new ConstraintBuilder(eventTypes, constraint.GetType());
                constraint.Define(builder);
                return builder.Build();
            }).ToImmutableList();
}
