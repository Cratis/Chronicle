// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.ReadModels;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.ReadModels;

internal static partial class SinkChangeReplaysLogging
{
    [LoggerMessage(LogLevel.Information, "Replaying observer '{ObserverId}' into the new sink of read model '{ReadModel}'")]
    internal static partial void ReplayingIntoChangedSink(this ILogger<SinkChangeReplays> logger, ObserverId observerId, ReadModelIdentifier readModel);

    [LoggerMessage(LogLevel.Warning, "Observer '{ObserverId}' refused to replay into the new sink of read model '{ReadModel}'; a replay is recommended instead")]
    internal static partial void ReplayIntoChangedSinkRefused(this ILogger<SinkChangeReplays> logger, ObserverId observerId, ReadModelIdentifier readModel, Exception exception);
}
