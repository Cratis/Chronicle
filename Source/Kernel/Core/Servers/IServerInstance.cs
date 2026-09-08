// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Servers;

namespace Cratis.Chronicle.Servers;

/// <summary>
/// Defines a system for reporting live resource usage for the server process running on a specific silo.
/// </summary>
/// <remarks>
/// The grain is keyed by the parsable string of the silo's <see cref="SiloAddress"/> and placed on
/// that silo, so the metrics it reports always describe the process actually running there. Use
/// <see cref="ServerInstanceGrainFactoryExtensions.GetServerInstance"/> to get the grain for a silo.
/// </remarks>
public interface IServerInstance : IGrainWithStringKey
{
    /// <summary>
    /// Gets a live snapshot of the server process' CPU and memory usage.
    /// </summary>
    /// <returns>The <see cref="ServerInstanceMetrics"/> for the process.</returns>
    Task<ServerInstanceMetrics> GetMetrics();
}
