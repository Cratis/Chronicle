// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Defines a projection that was registered explicitly rather than found by discovery.
/// </summary>
/// <remarks>
/// An explicit projection is held as a recipe rather than as a finished definition, because it may be declared before
/// the event types it refers to are known - a registration made while the host is being configured is built when the
/// event store discovers its artifacts, and is built again every time discovery runs.
/// </remarks>
internal interface IExplicitProjection
{
    /// <summary>
    /// Gets the type of read model the projection maintains.
    /// </summary>
    Type ReadModelType { get; }

    /// <summary>
    /// Gets the identifier of the projection.
    /// </summary>
    ProjectionId Id { get; }

    /// <summary>
    /// Gets a value indicating whether the projection is defined by model-bound attributes on the read model.
    /// </summary>
    bool IsModelBound { get; }

    /// <summary>
    /// Build the projection definition.
    /// </summary>
    /// <param name="context">The <see cref="ExplicitProjectionContext"/> to build within.</param>
    /// <returns>The built <see cref="ProjectionDefinition"/>.</returns>
    ProjectionDefinition Build(ExplicitProjectionContext context);
}
