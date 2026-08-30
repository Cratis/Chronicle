// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle;

internal static partial class EventStoreLogMessages
{
    [LoggerMessage(LogLevel.Trace, "Discover all artifacts")]
    internal static partial void DiscoverAllArtifacts(this ILogger logger);

    [LoggerMessage(LogLevel.Trace, "Register all artifacts")]
    internal static partial void RegisterAllArtifacts(this ILogger logger);

    [LoggerMessage(LogLevel.Warning, "Registering all artifacts failed on attempt {Attempt} of {MaxAttempts}. Waiting {Delay} before trying again")]
    internal static partial void RetryingRegisterAllArtifacts(this ILogger logger, Exception exception, int attempt, int maxAttempts, TimeSpan delay);
}
