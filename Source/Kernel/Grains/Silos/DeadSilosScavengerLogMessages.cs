// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Grains.Silos;

internal static partial class DeadSilosScavengerLogMessages
{
    [LoggerMessage(1, LogLevel.Information, "Removing dead silo {SiloAddress} from cluster info")]
    internal static partial void RemovingDeadSiloFromClusterInfo(this ILogger<DeadSilosScavenger> logger, string siloAddress);
}
