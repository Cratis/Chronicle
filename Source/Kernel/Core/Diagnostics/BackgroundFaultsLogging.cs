// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Diagnostics;

internal static partial class BackgroundFaultsLogging
{
    [LoggerMessage(LogLevel.Warning, "Background work '{Owner}' hit a transient failure and was abandoned; it is not retried here")]
    internal static partial void BackgroundWorkTransientFailure(this ILogger logger, Exception exception, string owner);

    [LoggerMessage(LogLevel.Error, "Background work '{Owner}' failed")]
    internal static partial void BackgroundWorkFailed(this ILogger logger, Exception exception, string owner);
}
