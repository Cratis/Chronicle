// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Services.Observation;

/// <summary>
/// Log messages for <see cref="SiloDepartures"/>.
/// </summary>
internal static partial class SiloDeparturesLogMessages
{
    [LoggerMessage(LogLevel.Information, "Silos {Silos} left the cluster. Checking the subscriptions of connected clients")]
    internal static partial void SilosLeftCheckingClientSubscriptions(this ILogger logger, IEnumerable<string> silos);

    [LoggerMessage(LogLevel.Warning, "Following the cluster membership failed. Client subscriptions are only checked at the safety-net interval until the silo restarts")]
    internal static partial void FollowingClusterMembershipFailed(this ILogger logger, Exception exception);
}
