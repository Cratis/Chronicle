// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Aksio.Cratis.Projections.Definitions;
using AutoMapper;

namespace Aksio.Cratis.Integration;

/// <summary>
/// Represents an implementation of <see cref="IAdapters"/>.
/// </summary>
[Singleton]
public class Adapters : IAdapters
{
    readonly Dictionary<AdapterKey, object> _artifacts = new();
    readonly IClientArtifactsProvider _clientArtifacts;
    readonly IServiceProvider _serviceProvider;
    readonly IAdapterProjectionFactory _adapterProjectionFactory;
    readonly IAdapterMapperFactory _adapterMapperFactory;
    readonly List<ProjectionDefinition> _projectionDefinitions = new();

    /// <inheritdoc/>
    public IEnumerable<ProjectionDefinition> Definitions => _projectionDefinitions;

    /// <summary>
    /// Initializes a new instance of the <see cref="Adapters"/> class.
    /// </summary>
    /// <param name="clientArtifacts">Optional <see cref="IClientArtifactsProvider"/> for the client artifacts.</param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> for getting instances from the IoC container.</param>
    /// <param name="adapterProjectionFactory"><see cref="IAdapterProjectionFactory"/> for creating projections for adapters.</param>
    /// <param name="adapterMapperFactory"><see cref="IAdapterMapperFactory"/> for creating mappers for adapters.</param>
    public Adapters(
        IClientArtifactsProvider clientArtifacts,
        IServiceProvider serviceProvider,
        IAdapterProjectionFactory adapterProjectionFactory,
        IAdapterMapperFactory adapterMapperFactory)
    {
        _clientArtifacts = clientArtifacts;
        _serviceProvider = serviceProvider;
        _adapterProjectionFactory = adapterProjectionFactory;
        _adapterMapperFactory = adapterMapperFactory;
        Initialize().GetAwaiter().GetResult();
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

    async Task Initialize()
    {
        var adapterArtifactsGenericType = typeof(AdapterArtifacts<,>);
        foreach (var adapterType in _clientArtifacts.Adapters)
        {
            var adapterInterface = adapterType.GetInterface(typeof(IAdapterFor<,>).Name)!;
            var adapterArtifactsType = adapterArtifactsGenericType.MakeGenericType(adapterInterface.GenericTypeArguments);
            var key = new AdapterKey(adapterInterface.GenericTypeArguments[0], adapterInterface.GenericTypeArguments[1]);
            var artifacts = Activator.CreateInstance(adapterArtifactsType, adapterType, _serviceProvider, _adapterProjectionFactory, _adapterMapperFactory);
            var method = adapterArtifactsType.GetMethod(nameof(AdapterArtifacts<object, object>.Initialize), BindingFlags.Public | BindingFlags.Instance);
            await (method!.Invoke(artifacts, Array.Empty<object>()) as Task)!;

            var projection = adapterArtifactsType.GetProperty(nameof(AdapterArtifacts<object, object>.Projection))!.GetValue(artifacts);
            var projectionDefinition = projection!.GetType().GetProperty(nameof(IAdapterProjectionFor<object>.Definition))!.GetValue(projection) as ProjectionDefinition;
            _projectionDefinitions.Add(projectionDefinition!);

            _artifacts[key] = artifacts!;
        }
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
