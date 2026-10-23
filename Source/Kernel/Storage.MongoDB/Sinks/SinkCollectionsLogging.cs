// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

internal static partial class SinkCollectionsLogging
{
    [LoggerMessage(LogLevel.Error, "A destructive {Operation} of collection '{Collection}' for read model '{ReadModel}' was refused")]
    internal static partial void SinkCollectionOperationRefused(this ILogger<SinkCollections> logger, SinkCollectionOperation operation, string collection, ReadModelIdentifier readModel, Exception exception);
}
