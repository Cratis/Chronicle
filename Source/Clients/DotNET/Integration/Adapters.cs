// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Types;
using AutoMapper;

namespace Aksio.Cratis.Integration;

/// <summary>
/// Represents an implementation of <see cref="IAdapters"/>.
/// </summary>
[Singleton]
public class Adapters : IAdapters
{
    readonly Dictionary<AdapterKey, object> _artifacts = new();
    readonly ITypes _types;
    readonly IServiceProvider _serviceProvider;
    readonly IAdapterProjectionFactory _adapterProjectionFactory;
    readonly IAdapterMapperFactory _adapterMapperFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="Adapters"/> class.
    /// </summary>
    /// <param name="types"><see cref="ITypes"/> for type discovery.</param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> for getting instances from the IoC container.</param>
    /// <param name="adapterProjectionFactory"><see cref="IAdapterProjectionFactory"/> for creating projections for adapters.</param>
    /// <param name="adapterMapperFactory"><see cref="IAdapterMapperFactory"/> for creating mappers for adapters.</param>
    public Adapters(
        ITypes types,
        IServiceProvider serviceProvider,
        IAdapterProjectionFactory adapterProjectionFactory,
        IAdapterMapperFactory adapterMapperFactory)
    {
        _types = types;
        _serviceProvider = serviceProvider;
        _adapterProjectionFactory = adapterProjectionFactory;
        _adapterMapperFactory = adapterMapperFactory;
    }

    /// <inheritdoc/>
    public async Task Initialize()
    {
        var adapterArtifactsGenericType = typeof(AdapterArtifacts<,>);
        foreach (var adapterType in _types.FindMultiple(typeof(IAdapterFor<,>)))
        {
            var adapterInterface = adapterType.GetInterface(typeof(IAdapterFor<,>).Name)!;
            var adapterArtifactsType = adapterArtifactsGenericType.MakeGenericType(adapterInterface.GenericTypeArguments);
            var key = new AdapterKey(adapterInterface.GenericTypeArguments[0], adapterInterface.GenericTypeArguments[1]);
            var artifacts = Activator.CreateInstance(adapterArtifactsType, adapterType, _serviceProvider, _adapterProjectionFactory, _adapterMapperFactory);
            var method = adapterArtifactsType.GetMethod(nameof(AdapterArtifacts<object, object>.Initialize), BindingFlags.Public | BindingFlags.Instance);
            await (method!.Invoke(artifacts, Array.Empty<object>()) as Task)!;
            _artifacts[key] = artifacts!;
        }
    }

    /// <inheritdoc/>
    public IAdapterFor<TModel, TExternalModel> GetFor<TModel, TExternalModel>()
    {
        return GetArtifactsFor<TModel, TExternalModel>().Adapter!;
    }

    /// <inheritdoc/>
    public IAdapterProjectionFor<TModel> GetProjectionFor<TModel, TExternalModel>()
    {
        return GetArtifactsFor<TModel, TExternalModel>().Projection!;
    }

    /// <inheritdoc/>
    public IMapper GetMapperFor<TModel, TExternalModel>()
    {
        return GetArtifactsFor<TModel, TExternalModel>().Mapper!;
    }

    AdapterArtifacts<TModel, TExternalModel> GetArtifactsFor<TModel, TExternalModel>()
    {
        ThrowIfMissingAdapterForModelAndExternalModel<TModel, TExternalModel>();
        return (AdapterArtifacts<TModel, TExternalModel>)_artifacts[new(typeof(TModel), typeof(TExternalModel))];
    }

    void ThrowIfMissingAdapterForModelAndExternalModel<TModel, TExternalModel>()
    {
        if (!_artifacts.ContainsKey(new(typeof(TModel), typeof(TExternalModel))))
        {
            throw new MissingAdapterForModelAndExternalModel(typeof(TModel), typeof(TExternalModel));
        }
    }
}
