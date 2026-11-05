// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams;

/// <summary>
/// Logging extensions for <see cref="ClosedStreamsConstraintStorage"/>.
/// </summary>
internal static partial class ClosedStreamsConstraintStorageLogging
{
    [LoggerMessage(LogLevel.Warning, "{Count} closed-stream entries in {EventSequenceId} were written before identifier comparison became exact and apply to every spelling until reviewed")]
    internal static partial void ClosedStreamEntriesPendingReview(this ILogger<ClosedStreamsConstraintStorage> logger, int count, string eventSequenceId);
}
