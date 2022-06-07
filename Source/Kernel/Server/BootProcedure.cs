// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Boot;
using Aksio.Cratis.Configuration;
using Aksio.Cratis.Events.Projections.Grains;
using Aksio.Cratis.Events.Schemas;
using Aksio.Cratis.Execution;
using Orleans;

namespace Aksio.Cratis.Server;

/// <summary>
/// Represents a <see cref="IPerformBootProcedure"/> for the event store.
/// </summary>
public class BootProcedure : IPerformBootProcedure
{
    readonly IServiceProvider _serviceProvider;
    readonly IExecutionContextManager _executionContextManager;
    readonly IGrainFactory _grainFactory;
    readonly Microservices _microservices;

    /// <summary>
    /// Initializes a new instance of the <see cref="BootProcedure"/> class.
    /// </summary>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> for getting services.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="grainFactory"><see cref="IGrainFactory"/> for getting grains.</param>
    /// <param name="microservices"><see cref="Microservices"/> configuration.</param>
    public BootProcedure(
        IServiceProvider serviceProvider,
        IExecutionContextManager executionContextManager,
        IGrainFactory grainFactory,
        Microservices microservices)
    {
        _serviceProvider = serviceProvider;
        _executionContextManager = executionContextManager;
        _grainFactory = grainFactory;
        _microservices = microservices;
    }

    /// <inheritdoc/>
    public void Perform()
    {
        foreach (var microserviceId in _microservices.GetMicroserviceIds())
        {
            _executionContextManager.Establish(microserviceId);
            var schemaStore = _serviceProvider.GetService<ISchemaStore>()!;
            schemaStore.Populate().Wait();

            var projections = _grainFactory.GetGrain<IProjections>(microserviceId);
            projections.Rehydrate().Wait();
        }
    }
}
