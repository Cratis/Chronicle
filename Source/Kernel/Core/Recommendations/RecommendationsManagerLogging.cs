// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Recommendations;

/// <summary>
/// Log messages for the recommendations manager.
/// </summary>
internal static partial class RecommendationsManagerLogging
{
    /// <summary>
    /// Logs the number of superseded ignored recommendations removed.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="count">The number removed.</param>
    [LoggerMessage(LogLevel.Information, "Pruned {Count} ignored recommendation(s) superseded by a newer stored version")]
    internal static partial void PrunedIgnoredRecommendations(this ILogger<RecommendationsManager> logger, int count);
}
