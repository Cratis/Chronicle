// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation;

internal static partial class ObserverStateGrainStorageProviderLogging
{
    [LoggerMessage(LogLevel.Error, "Cannot activate observer {Observer}: this kernel is below the observer downgrade floor. Initialized observers require the gate release or later (decision 0011)")]
    internal static partial void KernelBelowDowngradeFloor(this ILogger<ObserverStateGrainStorageProvider> logger, Exception exception, ObserverKey observer);
}
