// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration;

namespace Cratis.Chronicle.Specifications.Integration;

/// <summary>
/// Represents a <see cref="IAdapterProjectionFor{T}"/> for in-memory purpose.
/// </summary>
/// <typeparam name="TModel">Type of model.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="SpecificationAdapterProjectionFor{TModel}"/> class.
/// </remarks>
/// <param name="context"><see cref="ProjectionSpecificationContext{TModel}"/> for working with the projection.</param>
public class SpecificationAdapterProjectionFor<TModel>(ProjectionSpecificationContext<TModel> context) : IAdapterProjectionFor<TModel>, IDisposable
{
    readonly ProjectionSpecificationContext<TModel> _context = context;

    /// <inheritdoc/>
    public ProjectionDefinition Definition => _context.Definition;

    /// <inheritdoc/>
    public void Dispose() => _context.Dispose();

    /// <inheritdoc/>
    public async Task<AdapterProjectionResult<TModel>> GetById(ModelKey modelKey)
    {
        var result = await _context.GetById(modelKey);
        return new AdapterProjectionResult<TModel>(result.Model, result.AffectedProperties, result.ProjectedEventsCount);
    }
}
