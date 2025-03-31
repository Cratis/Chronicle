// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Grains.Jobs;
using Microsoft.Extensions.Logging;
namespace Cratis.Chronicle.Grains.Observation.Jobs;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

internal static partial class JobLogging
{
    [LoggerMessage(LogLevel.Warning, "Not all events were handled after performing '{JobName}' job. The last handled event sequence number was {LastSequenceNumber}")]
    internal static partial void NotAllEventsWereHandled(this ILogger<IJob> logger, string jobName, EventSequenceNumber lastSequenceNumber);

    [LoggerMessage(LogLevel.Warning, "No  were handled after performing '{JobName}' job")]
    internal static partial void NoEventsWereHandled(this ILogger<IJob> logger, string jobName);
}
