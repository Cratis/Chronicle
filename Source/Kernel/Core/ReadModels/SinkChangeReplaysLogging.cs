// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.ReadModels;

internal static partial class SinkChangeReplaysLogging
{
    [LoggerMessage(LogLevel.Information, "Replaying read model '{ReadModel}' in namespace '{Namespace}' into its new sink")]
    internal static partial void ReplayingIntoChangedSink(this ILogger<SinkChangeReplays> logger, ReadModelIdentifier readModel, EventStoreNamespaceName @namespace);

    [LoggerMessage(LogLevel.Warning, "Replaying read model '{ReadModel}' in namespace '{Namespace}' into its new sink was refused; the replay stays recommended")]
    internal static partial void ReplayIntoChangedSinkRefused(this ILogger<SinkChangeReplays> logger, ReadModelIdentifier readModel, EventStoreNamespaceName @namespace, Exception exception);
}
