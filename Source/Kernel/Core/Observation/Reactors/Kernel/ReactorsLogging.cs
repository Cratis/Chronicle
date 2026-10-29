// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Reactors.Kernel;

internal static partial class ReactorsLogging
{
    [LoggerMessage(LogLevel.Warning, "Retrying system reactor subscription after deactivating an observer in a mixed-release cluster; attempt {Attempt}; cluster releases: {ClusterReleases}")]
    internal static partial void RetryingMixedReleaseSubscription(this ILogger<Reactors> logger, int attempt, string clusterReleases);
}
