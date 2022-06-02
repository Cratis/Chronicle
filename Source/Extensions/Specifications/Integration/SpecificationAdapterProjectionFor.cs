// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Projections;
using Aksio.Cratis.Integration;

namespace Aksio.Cratis.Specifications.Integration;

/// <summary>
/// Represents a <see cref="IAdapterProjectionFor{T}"/> for in-memory purpose.
/// </summary>
/// <typeparam name="TModel">Type of model.</typeparam>
public class SpecificationAdapterProjectionFor<TModel> : IAdapterProjectionFor<TModel>, IDisposable
{
    readonly ProjectionSpecificationContext<TModel> _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpecificationAdapterProjectionFor{TModel}"/> class.
    /// </summary>
    /// <param name="context"><see cref="ProjectionSpecificationContext{TModel}"/> for working with the projection.</param>
    public SpecificationAdapterProjectionFor(ProjectionSpecificationContext<TModel> context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public void Dispose() => _context.Dispose();

    /// <inheritdoc/>
    public Task<TModel> GetById(ModelKey modelKey) => _context.GetById(modelKey);
}
