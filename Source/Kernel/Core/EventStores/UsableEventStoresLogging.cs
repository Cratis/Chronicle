// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.EventStores;

/// <summary>
/// Holds log messages for <see cref="UsableEventStores"/>.
/// </summary>
internal static partial class UsableEventStoresLogging
{
    [LoggerMessage(LogLevel.Warning, "Leaving out the quarantined event store '{EventStore}'. It is reached once its name collision is resolved")]
    internal static partial void LeavingOutQuarantinedEventStore(this ILogger logger, string eventStore);

    [LoggerMessage(LogLevel.Warning, "Leaving out the quarantined namespace '{Namespace}' of event store '{EventStore}'. It is reached once its name collision is resolved")]
    internal static partial void LeavingOutQuarantinedNamespace(this ILogger logger, string @namespace, string eventStore);
}
