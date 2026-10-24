// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation;

internal static partial class PartitionReplayReconciliationLogMessages
{
    [LoggerMessage(LogLevel.Warning, "Recovering orphaned partition replay for observer '{Observer}' partition '{Partition}' from the first event")]
    internal static partial void RecoveringOrphanedPartitionReplay(this ILogger logger, ObserverId observer, Key partition);

    [LoggerMessage(LogLevel.Warning, "Could not reconcile partition replays for observer '{Observer}'")]
    internal static partial void PartitionReplayReconciliationFailed(this ILogger logger, Exception exception, ObserverId observer);
}
