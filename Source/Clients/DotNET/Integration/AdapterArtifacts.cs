// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using AutoMapper;

namespace Cratis.Chronicle.Integration;

/// <summary>
/// Represents an <see cref="IAdapterFor{TModel, TExternalModel}"/> and artifacts used with it.
/// </summary>
/// <typeparam name="TModel">Type of model.</typeparam>
/// <typeparam name="TExternalModel">Type of external model.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="AdapterArtifacts{TModel, TExternalModel}"/> class.
/// </remarks>
/// <param name="adapterType">Type of adapter.</param>
/// <param name="serviceProvider"><see cref="IServiceProvider"/> for getting services.</param>
/// <param name="adapterProjectionFactory"><see cref="IAdapterProjectionFactory"/> for creating projections for adapters.</param>
/// <param name="adapterMapperFactory"><see cref="IAdapterMapperFactory"/> for creating adapter mappers.</param>
public class AdapterArtifacts<TModel, TExternalModel>(
    Type adapterType,
    IServiceProvider serviceProvider,
    IAdapterProjectionFactory adapterProjectionFactory,
    IAdapterMapperFactory adapterMapperFactory)
{
    /// <summary>
    /// Gets the adapter.
    /// </summary>
    public IAdapterFor<TModel, TExternalModel>? Adapter { get; private set; }

    /// <summary>
    /// Gets the projection.
    /// </summary>
    public IAdapterProjectionFor<TModel>? Projection { get; private set; }

    /// <summary>
    /// Gets the mapper.
    /// </summary>
    public IMapper? Mapper { get; private set; }

    /// <summary>
    /// Initialize all artifacts.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    public async Task Initialize()
    {
        Adapter = serviceProvider.GetService(adapterType) as IAdapterFor<TModel, TExternalModel>;
        Projection = await adapterProjectionFactory.CreateFor(Adapter!);
        Mapper = adapterMapperFactory.CreateFor(Adapter!);
    }
}
