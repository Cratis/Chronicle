// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Jobs;

internal static partial class JobStepThrottleLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Acquiring job step slot")]
    internal static partial void AcquiringJobStepSlot(this ILogger<JobStepThrottle> logger);

    [LoggerMessage(LogLevel.Debug, "Releasing job step slot")]
    internal static partial void ReleasingJobStepSlot(this ILogger<JobStepThrottle> logger);
}
