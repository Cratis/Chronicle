// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Boot;
using Aksio.Cratis.Execution;
using Orleans;

namespace Aksio.Cratis.Events.Projections.Grains;

/// <summary>
/// Represents a <see cref="IPerformBootProcedure"/> for the event store.
/// </summary>
public class BootProcedure : IPerformBootProcedure
{
    readonly IGrainFactory _grainFactory;
    readonly IExecutionContextManager _executionContextManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="BootProcedure"/> class.
    /// </summary>
    /// <param name="grainFactory"><see cref="IGrainFactory"/> for getting grains.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    public BootProcedure(IGrainFactory grainFactory, IExecutionContextManager executionContextManager)
    {
        _grainFactory = grainFactory;
        _executionContextManager = executionContextManager;
    }

    /// <inheritdoc/>
    public void Perform()
    {
        // TODO: Start for all Microservices
        _executionContextManager.Establish(MicroserviceId.Unspecified);

        _ = _grainFactory.GetGrain<IProjections>(MicroserviceId.Unspecified);
    }
}
