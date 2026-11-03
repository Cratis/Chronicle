// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Kernel.Integration.Projections.for_ImmediateProjection.given;

/// <summary>
/// Defines a passive projection whose definition can change during an immediate read.
/// </summary>
public class InterleavedProjection : IProjectionFor<InterleavedReadModel>
{
    /// <summary>
    /// The projection identifier used by the kernel calls.
    /// </summary>
    public const string ProjectionIdentifier = "definition-change-interleave";

    /// <summary>
    /// Gets the projection identifier.
    /// </summary>
    public ProjectionId Identifier => ProjectionIdentifier;

    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<InterleavedReadModel> builder) => builder
        .Passive()
        .NoAutoMap()
        .WithInitialValues(() => new InterleavedReadModel(string.Empty, "original"))
        .From<ProjectionReadStarted>(from => from.Set(model => model.Name).To(@event => @event.Name));
}
