// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Models;

namespace Cratis.Chronicle.Integration;

/// <summary>
/// Defines a projection for an adapter for a specific model.
/// </summary>
/// <typeparam name="TModel">Type of model.</typeparam>
public interface IAdapterProjectionFor<TModel>
{
    /// <summary>
    /// Gets the <see cref="Definition"/> for the adapter.
    /// </summary>
    ProjectionDefinition Definition { get; }

    /// <summary>
    /// Get an instance by <see cref="ModelKey"/>.
    /// </summary>
    /// <param name="modelKey">The <see cref="ModelKey"/> to get for.</param>
    /// <returns>Instance of the model.</returns>
    Task<AdapterProjectionResult<TModel>> GetById(ModelKey modelKey);
}
