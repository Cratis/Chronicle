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

    [LoggerMessage(LogLevel.Warning, "Registering all artifacts exhausted its retries while the connection stayed healthy. Some observers may be left unsubscribed - continuing to retry in the background until it succeeds or the connection drops")]
    internal static partial void EnteringBackgroundRegistrationRetry(this ILogger logger);

    [LoggerMessage(LogLevel.Warning, "Registering all artifacts failed again on background attempt {Attempt}. Waiting {Delay} before trying again")]
    internal static partial void RetryingRegisterAllArtifactsInBackground(this ILogger logger, Exception exception, int attempt, TimeSpan delay);

    [LoggerMessage(LogLevel.Information, "Registering all artifacts succeeded after {Attempt} background attempt(s)")]
    internal static partial void BackgroundRegistrationRetrySucceeded(this ILogger logger, int attempt);
}
