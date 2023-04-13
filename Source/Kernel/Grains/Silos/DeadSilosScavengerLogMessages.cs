// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Grains.Silos;

internal static partial class DeadSilosScavengerLogMessages
{
    [LoggerMessage(1, LogLevel.Information, "Looking for dead silos")]
    internal static partial void LookForDeadSilos(this ILogger<DeadSilosScavenger> logger);

    [LoggerMessage(2, LogLevel.Information, "Removing dead silo {SiloAddress} from cluster info")]
    internal static partial void RemovingDeadSiloFromClusterInfo(this ILogger<DeadSilosScavenger> logger, string siloAddress);

    [LoggerMessage(3, LogLevel.Information, "No dead silos found")]
    internal static partial void NoDeadSilos(this ILogger<DeadSilosScavenger> logger);
}
