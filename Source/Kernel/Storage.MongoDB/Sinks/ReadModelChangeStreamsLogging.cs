// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

internal static partial class ReadModelChangeStreamsLogging
{
    [LoggerMessage(LogLevel.Warning, "Reading observed collection '{Collection}' failed (attempt {Attempt}); retrying")]
    internal static partial void ReadingObservedCollectionFailed(this ILogger<ReadModelChangeStreams> logger, string collection, int attempt, Exception exception);

    [LoggerMessage(LogLevel.Warning, "The change stream observing collection '{Collection}' failed (attempt {Attempt}); reopening it")]
    internal static partial void ChangeStreamFailed(this ILogger<ReadModelChangeStreams> logger, string collection, int attempt, Exception exception);

    [LoggerMessage(LogLevel.Error, "The change stream observing collection '{Collection}' failed and was not reopened; its observers are ended")]
    internal static partial void ChangeStreamGaveUp(this ILogger<ReadModelChangeStreams> logger, string collection, Exception exception);
}
