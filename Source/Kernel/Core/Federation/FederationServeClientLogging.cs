// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Log messages for <see cref="FederationServeClient"/>. Never logs certificate material or event content.
/// </summary>
internal static partial class FederationServeClientLogging
{
    [LoggerMessage(LogLevel.Warning, "The federation authority refused this kernel's outbound direction; trying again later while the other direction keeps running")]
    internal static partial void ServeRefused(this ILogger<FederationServeClient> logger);

    [LoggerMessage(LogLevel.Warning, "The federation serve call failed ({ExceptionType}); trying again in {Backoff}")]
    internal static partial void ServeCallFailed(this ILogger<FederationServeClient> logger, string exceptionType, TimeSpan backoff);
}
