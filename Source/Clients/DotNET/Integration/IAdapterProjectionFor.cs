// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Projections;

namespace Aksio.Cratis.Integration;

/// <summary>
/// Defines a projection for an adapter for a specific model.
/// </summary>
/// <typeparam name="TModel">Type of model.</typeparam>
public interface IAdapterProjectionFor<TModel>
{
    /// <summary>
    /// Get an instance by <see cref="ModelKey"/>.
    /// </summary>
    /// <param name="modelKey">The <see cref="ModelKey"/> to get for.</param>
    /// <returns>Instance of the model.</returns>
    Task<AdapterProjectionResult<TModel>> GetById(ModelKey modelKey);
}
