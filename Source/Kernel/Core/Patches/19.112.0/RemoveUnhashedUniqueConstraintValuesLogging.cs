// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Patches;

internal static partial class RemoveUnhashedUniqueConstraintValuesLogging
{
    [LoggerMessage(LogLevel.Information, "Removed {PerEventSource} per-event-source and {PerValue} per-value unhashed unique-constraint claims from {EventStore}/{Namespace}/{EventSequence}; SQL column and index repair completed")]
    internal static partial void RemovedUnhashedValues(this ILogger<RemoveUnhashedUniqueConstraintValues> logger, EventStoreName eventStore, EventStoreNamespaceName @namespace, EventSequenceId eventSequence, long perEventSource, long perValue);

    [LoggerMessage(LogLevel.Warning, "Unhashed unique-constraint claims cannot be restored by rollback. Restore the backup taken before upgrading; reindexing rebuilds current hashes, not deleted rows")]
    internal static partial void RestoreBackupRequired(this ILogger<RemoveUnhashedUniqueConstraintValues> logger);
}
