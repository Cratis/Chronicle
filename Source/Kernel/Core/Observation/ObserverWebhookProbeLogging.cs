// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation;

internal static partial class ObserverWebhookProbeLogging
{
    [LoggerMessage(LogLevel.Information, "Webhook probe {ProbeId} scheduled for {Due} after {Failures} failed probes")]
    internal static partial void WebhookProbeScheduled(this ILogger<Observer> logger, Guid probeId, DateTimeOffset due, int failures);

    [LoggerMessage(LogLevel.Information, "Webhook probe {ProbeId} attempting failure episode {EpisodeId}")]
    internal static partial void WebhookProbeAttempt(this ILogger<Observer> logger, Guid probeId, FailedPartitionId episodeId);

    [LoggerMessage(LogLevel.Warning, "Webhook probe could not finish local coordination; quarantine retained")]
    internal static partial void WebhookProbeLocalFailure(this ILogger<Observer> logger);

    [LoggerMessage(LogLevel.Information, "Webhook probe {ProbeId} rescheduled for {Due}, reason {Reason}")]
    internal static partial void WebhookProbeRescheduled(this ILogger<Observer> logger, Guid probeId, string reason, DateTimeOffset due);

    [LoggerMessage(LogLevel.Information, "Webhook probe {ProbeId} resumed observer delivery")]
    internal static partial void WebhookProbeResumed(this ILogger<Observer> logger, Guid probeId);

    [LoggerMessage(LogLevel.Information, "Webhook probe {ProbeId} invalidated by operator lifecycle change")]
    internal static partial void WebhookProbeCancelled(this ILogger<Observer> logger, Guid probeId);
}
