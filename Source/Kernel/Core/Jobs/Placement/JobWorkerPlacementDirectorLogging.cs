// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Jobs.Placement;

internal static partial class JobWorkerPlacementDirectorLogging
{
    [LoggerMessage(LogLevel.Warning, "No compatible silo has known or unknown Jobs roles; placing job workers on silos without the Jobs role")]
    internal static partial void EnteringFallback(this ILogger<JobWorkerPlacementDirector> logger);

    [LoggerMessage(LogLevel.Warning, "Job worker placement is no longer falling back to silos without the Jobs role")]
    internal static partial void LeavingFallback(this ILogger<JobWorkerPlacementDirector> logger);
}
