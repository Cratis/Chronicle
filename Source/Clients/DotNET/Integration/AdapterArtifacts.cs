// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using AutoMapper;

namespace Aksio.Cratis.Integration;

/// <summary>
/// Represents an <see cref="IAdapterFor{TModel, TExternalModel}"/> and artifacts used with it.
/// </summary>
/// <typeparam name="TModel">Type of model.</typeparam>
/// <typeparam name="TExternalModel">Type of external model.</typeparam>
public class AdapterArtifacts<TModel, TExternalModel>
{
    readonly Type _adapterType;
    readonly IServiceProvider _serviceProvider;
    readonly IAdapterProjectionFactory _adapterProjectionFactory;
    readonly IAdapterMapperFactory _adapterMapperFactory;

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
    /// Initializes a new instance of the <see cref="AdapterArtifacts{TModel, TExternalModel}"/> class.
    /// </summary>
    /// <param name="adapterType">Type of adapter.</param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> for getting services.</param>
    /// <param name="adapterProjectionFactory"><see cref="IAdapterProjectionFactory"/> for creating projections for adapters.</param>
    /// <param name="adapterMapperFactory"><see cref="IAdapterMapperFactory"/> for creating adapter mappers.</param>
    public AdapterArtifacts(
        Type adapterType,
        IServiceProvider serviceProvider,
        IAdapterProjectionFactory adapterProjectionFactory,
        IAdapterMapperFactory adapterMapperFactory)
    {
        _adapterType = adapterType;
        _serviceProvider = serviceProvider;
        _adapterProjectionFactory = adapterProjectionFactory;
        _adapterMapperFactory = adapterMapperFactory;
    }

    /// <summary>
    /// Initialize all artifacts.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    public async Task Initialize()
    {
        Adapter = _serviceProvider.GetService(_adapterType) as IAdapterFor<TModel, TExternalModel>;
        Projection = await _adapterProjectionFactory.CreateFor(Adapter!);
        Mapper = _adapterMapperFactory.CreateFor(Adapter!);
    }
}
