// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using AutoMapper;

namespace Aksio.Cratis.Integration;

/// <summary>
/// Defines the system for working with <see cref="AdapterFor{TModel, TExternalModel}"/>.
/// </summary>
public interface IAdapters
{
    /// <summary>
    /// Initialize all adapters.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Initialize();

    /// <summary>
    /// Gets an <see cref="IAdapterFor{TModel, TExternalModel}"/> for the specific model and external model.
    /// </summary>
    /// <typeparam name="TModel">Type of model.</typeparam>
    /// <typeparam name="TExternalModel">Type of external model.</typeparam>
    /// <returns>The <see cref="AdapterFor{TModel, TExternalModel}"/>.</returns>
    IAdapterFor<TModel, TExternalModel> GetFor<TModel, TExternalModel>();

    /// <summary>
    /// Gets a <see cref="IAdapterProjectionFor{TModel}"/> for a specific model and external model.
    /// </summary>
    /// <typeparam name="TModel">Type of model.</typeparam>
    /// <typeparam name="TExternalModel">Type of external model.</typeparam>
    /// <returns><see cref="IAdapterProjectionFor{TModel}"/> instance.</returns>
    IAdapterProjectionFor<TModel> GetProjectionFor<TModel, TExternalModel>();

    /// <summary>
    /// Get the mapper for a specific combination of model and external model.
    /// </summary>
    /// <typeparam name="TModel">Type of model.</typeparam>
    /// <typeparam name="TExternalModel">Type of external model.</typeparam>
    /// <returns><see cref="IMapper"/> instance.</returns>
    IMapper GetMapperFor<TModel, TExternalModel>();
}
