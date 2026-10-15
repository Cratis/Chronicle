// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.EventSequences;

internal static partial class EventSequenceLogging
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "An incomplete explicit concurrency scope has no narrowing and will not be checked. Use ConcurrencyScope.None to append without a check, or specify narrowing and an expected boundary.")]
    internal static partial void IncompleteScopeWithoutNarrowing(this ILogger<EventSequence> logger);
}
