// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Boot;
using Orleans;

namespace Aksio.Cratis.Kernel.Grains.Silos;

/// <summary>
/// Represents a <see cref="IPerformBootProcedure"/> that initializes related work for Silos.
/// </summary>
public class BootProcedure : IPerformBootProcedure
{
    readonly IGrainFactory _grainFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="BootProcedure"/> class.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> for working with grains.</param>
    public BootProcedure(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory;
    }

    /// <inheritdoc/>
    public void Perform()
    {
        _grainFactory.GetGrain<IDeadSilosScavenger>(Guid.Empty).Start().Wait();
    }
}
