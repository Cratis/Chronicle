// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Boot;
using Aksio.Cratis.Configuration;
using Orleans;

namespace Aksio.Cratis.Events.Projections.Grains;

/// <summary>
/// Represents a <see cref="IPerformBootProcedure"/> for the event store.
/// </summary>
public class BootProcedure : IPerformBootProcedure
{
    readonly IGrainFactory _grainFactory;
    readonly Microservices _microservices;

    /// <summary>
    /// Initializes a new instance of the <see cref="BootProcedure"/> class.
    /// </summary>
    /// <param name="grainFactory"><see cref="IGrainFactory"/> for getting grains.</param>
    /// <param name="microservices"><see cref="Microservices"/> configuration.</param>
    public BootProcedure(
        IGrainFactory grainFactory,
        Microservices microservices)
    {
        _grainFactory = grainFactory;
        _microservices = microservices;
    }

    /// <inheritdoc/>
    public void Perform()
    {
        foreach (var microserviceId in _microservices.GetMicroserviceIds())
        {
            _ = _grainFactory.GetGrain<IProjections>(microserviceId);
        }
    }
}
