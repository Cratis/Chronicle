// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Log messages for <see cref="Sink"/>.
/// </summary>
/// <remarks>
/// The exceptions and the error details documents are deliberately not logged: both can echo the document that was
/// being written, which can hold personal data. Error codes and the server's error messages are the diagnostic value.
/// </remarks>
internal static partial class SinkLogging
{
    [LoggerMessage(LogLevel.Error, "Bulk write to collection '{Collection}' for read model '{ReadModel}' failed for partition '{Partition}' at operation {OperationIndex}: error {Code} ({Category}): {ErrorMessage}")]
    internal static partial void BulkWriteErrorOccurred(this ILogger<Sink> logger, string collection, ReadModelIdentifier readModel, string partition, int operationIndex, int code, ServerErrorCategory category, string errorMessage);

    [LoggerMessage(LogLevel.Error, "Bulk write to collection '{Collection}' for read model '{ReadModel}' reported a write concern error {Code} ({CodeName}): {ErrorMessage} - the outcome of {OperationCount} operations is unknown")]
    internal static partial void BulkWriteConcernErrorOccurred(this ILogger<Sink> logger, string collection, ReadModelIdentifier readModel, int code, string codeName, string errorMessage, int operationCount);

    [LoggerMessage(LogLevel.Warning, "Replay of read model '{ReadModel}' ended with {FailedPartitionCount} failed partitions in its final flush; the rebuilt collection is promoted and the failed partitions are recorded for retry")]
    internal static partial void EndingReplayWithFailedPartitions(this ILogger<Sink> logger, ReadModelIdentifier readModel, int failedPartitionCount);

    [LoggerMessage(LogLevel.Warning, "Bulk write for read model '{ReadModel}' failed before entering replay: partition '{Partition}' at event {SequenceNumber}: {Reason}")]
    internal static partial void BulkFailedBeforeReplay(this ILogger<Sink> logger, ReadModelIdentifier readModel, Key partition, EventSequenceNumber sequenceNumber, string reason);

    [LoggerMessage(LogLevel.Warning, "The final flush of the replay of read model '{ReadModel}' failed; the sink leaves replay mode without promoting the rebuilt collection")]
    internal static partial void AbandoningReplayAfterFailedFlush(this ILogger<Sink> logger, ReadModelIdentifier readModel);
}
