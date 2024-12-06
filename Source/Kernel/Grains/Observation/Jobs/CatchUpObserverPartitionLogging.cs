// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
namespace Cratis.Chronicle.Grains.Observation.Jobs;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

internal static partial class CatchUpObserverPartitionLogging
{
    [LoggerMessage(LogLevel.Debug, "Not all steps was completed successfully. Will not notify observer of partition being caught up")]
    internal static partial void AllStepsNotCompletedSuccessfully(this ILogger<CatchUpObserverPartition> logger);
}
