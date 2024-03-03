// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Changes;
using Aksio.Cratis.Json;
using Aksio.Cratis.Kernel.Grains.Observation.Reducers;
using Aksio.Cratis.Kernel.Grains.Projections;
using Aksio.Cratis.Kernel.Grains.Projections.Pipelines;
using Aksio.Cratis.Kernel.Projections;
using Aksio.Cratis.Kernel.Projections.Expressions;
using Aksio.Cratis.Kernel.Projections.Expressions.EventValues;
using Aksio.Cratis.Kernel.Projections.Expressions.Keys;
using Aksio.Cratis.Kernel.Storage;
using Aksio.Cratis.Kernel.Storage.Sinks;
using Aksio.Cratis.Schemas;
using Aksio.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Grains;

/// <summary>
/// Represents an implementation of <see cref="IEventStoreNamespace"/>.
/// </summary>
public class EventStoreNamespace : IEventStoreNamespace
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventStoreNamespace"/> class.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/>.</param>
    /// <param name="name">The <see cref="EventStoreNamespaceName"/>.</param>
    /// <param name="storage"><see cref="IEventStoreNamespaceStorage"/> for accessing underlying storage for the specific namespace.</param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> for getting service instances.</param>
    /// <param name="loggerFactory"><see cref="ILoggerFactory"/> for creating loggers.</param>
    public EventStoreNamespace(
        EventStoreName eventStore,
        EventStoreNamespaceName name,
        IEventStoreNamespaceStorage storage,
        IServiceProvider serviceProvider,
        ILoggerFactory loggerFactory)
    {
        Name = name;
        Storage = storage;

        Sinks = new Storage.Sinks.Sinks(eventStore, name, serviceProvider.GetRequiredService<IInstancesOf<ISinkFactory>>());
        var projectionFactory = new ProjectionFactory(
            serviceProvider.GetRequiredService<IModelPropertyExpressionResolvers>(),
            serviceProvider.GetRequiredService<IEventValueProviderExpressionResolvers>(),
            serviceProvider.GetRequiredService<IKeyExpressionResolvers>(),
            serviceProvider.GetRequiredService<IExpandoObjectConverter>(),
            storage);

        var pipelineFactory = new ProjectionPipelineFactory(
            Sinks,
            storage,
            serviceProvider.GetRequiredService<IObjectComparer>(),
            serviceProvider.GetRequiredService<ITypeFormats>(),
            loggerFactory);

        ProjectionManager = new ProjectionManager(
            eventStore,
            name,
            projectionFactory,
            pipelineFactory,
            loggerFactory.CreateLogger<ProjectionManager>());

        var sinks = new Storage.Sinks.Sinks(
            eventStore,
            name,
            serviceProvider.GetRequiredService<IInstancesOf<ISinkFactory>>());
        ReducerPipelines = new ReducerPipelines(
            sinks,
            serviceProvider.GetRequiredService<IObjectComparer>());
    }

    /// <inheritdoc/>
    public EventStoreNamespaceName Name { get; }

    /// <inheritdoc/>
    public IEventStoreNamespaceStorage Storage { get; }

    /// <inheritdoc/>
    public IProjectionManager ProjectionManager { get; }

    /// <inheritdoc/>
    public IReducerPipelines ReducerPipelines { get; }

    /// <inheritdoc/>
    public ISinks Sinks { get; }
}
