// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Domain.Observation;

internal static partial class ObserversLogMessages
{
    [LoggerMessage(0, LogLevel.Information, "Registering observers")]
    internal static partial void RegisterObservers(this ILogger<Observers> logger);

    [LoggerMessage(1, LogLevel.Information, "Observers registered, it took {Elapsed}")]
    internal static partial void ObserversRegistered(this ILogger<Observers> logger, TimeSpan elapsed);
}
