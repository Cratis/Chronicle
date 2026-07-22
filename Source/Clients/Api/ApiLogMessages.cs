// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api;

internal static partial class ApiLogMessages
{
    [LoggerMessage(LogLevel.Critical, "Unhandled exception occurred (terminating: {IsTerminating})")]
    internal static partial void UnhandledException(this ILogger<ChronicleApi> logger, Exception exception, bool isTerminating);

    [LoggerMessage(LogLevel.Error, "Unobserved task exception occurred")]
    internal static partial void UnobservedTaskException(this ILogger<ChronicleApi> logger, Exception exception);
}
