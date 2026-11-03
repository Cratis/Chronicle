// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation;

#pragma warning disable SA1600 // Elements should be documented

internal static partial class PartitionRebuildAdmissionLogging
{
    [LoggerMessage(LogLevel.Warning, "Could not decide whether partition {Partition} of observer {ObserverId} can be rebuilt in isolation; replaying it in place")]
    internal static partial void CouldNotDecideIsolatedRebuild(this ILogger<PartitionRebuildAdmission> logger, ObserverId observerId, Key partition, Exception exception);
}
