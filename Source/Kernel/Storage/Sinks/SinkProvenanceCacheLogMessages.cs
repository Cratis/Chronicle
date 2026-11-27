// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Holds diagnostic messages for database-local provenance refresh and shutdown.
/// </summary>
internal static partial class SinkProvenanceCacheLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Read-model metadata refresh is unavailable")]
    internal static partial void RefreshFailed(this ILogger<SinkProvenanceCache> logger, Exception error);

    [LoggerMessage(LogLevel.Warning, "Read-model provenance revocation subscriber failed")]
    internal static partial void RevocationFailed(this ILogger<SinkProvenanceCache> logger, Exception error);

    [LoggerMessage(LogLevel.Debug, "Read-model metadata work failed during shutdown")]
    internal static partial void ShutdownReadFailed(this ILogger<SinkProvenanceCache> logger, Exception error);
}
