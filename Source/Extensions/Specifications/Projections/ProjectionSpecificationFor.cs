// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Projections;
using Aksio.Specifications;

namespace Aksio.Cratis.Specifications.Integration;

/// <summary>
/// Represents a specialized <see cref="Specification"/> for specifying behaviors of adapters.
/// </summary>
/// <typeparam name="TModel">The model the projection is for.</typeparam>
public abstract class ProjectionSpecificationFor<TModel> : Specification
{
    /// <summary>
    /// Gets the <see cref="IProjectionFor{TModel}"/> instance.
    /// </summary>
    protected IProjectionFor<TModel> projection { get; private set; }

    /// <summary>
    /// Gets the <see cref="ProjectionSpecificationContext{TModel}"/>.
    /// </summary>
    protected ProjectionSpecificationContext<TModel> context { get; private set; }

    /// <summary>
    /// Create an instance of the projection for the specification.
    /// </summary>
    /// <returns>A new instance of the projection.</returns>
    protected abstract IProjectionFor<TModel> CreateProjection();

    void Establish()
    {
        projection = CreateProjection();
        context = new(projection.Identifier, projection.Define);
    }
}
