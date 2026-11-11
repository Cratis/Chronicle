// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Services.Observation;

/// <summary>
/// Log messages for <see cref="SiloDepartures"/>.
/// </summary>
internal static partial class SiloDeparturesLogMessages
{
    [LoggerMessage(LogLevel.Information, "Silos {Silos} are leaving or left the cluster. Checking the subscriptions of connected clients")]
    internal static partial void SilosLeavingCheckingClientSubscriptions(this ILogger logger, IEnumerable<string> silos);

    [LoggerMessage(LogLevel.Warning, "Following the cluster membership failed. Following it again in {Retry}; until then client subscriptions are only checked at the safety-net interval")]
    internal static partial void FollowingClusterMembershipFailed(this ILogger logger, TimeSpan retry, Exception exception);
}
