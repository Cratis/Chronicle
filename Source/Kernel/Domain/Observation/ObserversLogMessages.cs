// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Domain.Observation;

internal static partial class ObserversLogMessages
{
    [LoggerMessage(0, LogLevel.Information, "Registering observers")]
    internal static partial void RegisterObservers(this ILogger<Observers> logger);

    [LoggerMessage(1, LogLevel.Information, "Waiting for observers to be ready")]
    internal static partial void WaitingForObserversToBeReady(this ILogger<Observers> logger);

    [LoggerMessage(2, LogLevel.Information, "Retrying waiting for observers to be ready")]
    internal static partial void RetryWaitingForObserversToBeReady(this ILogger<Observers> logger);

    [LoggerMessage(3, LogLevel.Information, "Reached maximum retries waiting for observers to be ready: {MaxRetries}")]
    internal static partial void ReachedMaximumRetriesWaitingForObserversToBeReady(this ILogger<Observers> logger, int maxRetries);

    [LoggerMessage(4, LogLevel.Information, "All observers are active")]
    internal static partial void AllObserversAreActive(this ILogger<Observers> logger);
}
