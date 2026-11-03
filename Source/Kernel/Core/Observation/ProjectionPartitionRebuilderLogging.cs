// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation;

#pragma warning disable SA1600 // Elements should be documented

internal static partial class ProjectionPartitionRebuilderLogging
{
    [LoggerMessage(LogLevel.Debug, "Began rebuilding partition {Partition} of projection {ObserverId} in isolation")]
    internal static partial void BeganIsolatedRebuild(this ILogger<ProjectionPartitionRebuilder> logger, ObserverId observerId, Key partition);

    [LoggerMessage(LogLevel.Warning, "Rebuilding partition {Partition} of projection {ObserverId} in isolation failed")]
    internal static partial void FailedIsolatedRebuild(this ILogger<ProjectionPartitionRebuilder> logger, Exception exception, ObserverId observerId, Key partition);

    [LoggerMessage(LogLevel.Debug, "Publishing the isolated rebuild of partition {Partition} of projection {ObserverId} ended as {Result}")]
    internal static partial void PublishedIsolatedRebuild(this ILogger<ProjectionPartitionRebuilder> logger, ObserverId observerId, Key partition, IsolatedReplaceResult result);
}
