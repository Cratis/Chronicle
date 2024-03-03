// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Kernel.Grains.Observation;

#pragma warning disable SA1600
namespace Cratis.Kernel.Server;

/// <summary>
/// Extension methods for replay state management initialization.
/// </summary>
public static class ReplayStateManagementExtensions
{
    /// <summary>
    /// Add replay state management.
    /// </summary>
    /// <param name="siloBuilder"><see cref="ISiloBuilder"/> to configure for.</param>
    /// <returns><see cref="ISiloBuilder"/> for continuation.</returns>
    public static ISiloBuilder AddReplayStateManagement(this ISiloBuilder siloBuilder)
    {
        siloBuilder.AddGrainService<ObserverService>();
        siloBuilder.ConfigureServices(_ => _.AddSingleton<IObserverServiceClient, ObserverServiceClient>());
        return siloBuilder;
    }
}
