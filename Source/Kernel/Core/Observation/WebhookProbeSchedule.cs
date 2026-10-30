// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Calculates bounded probe deadlines independently of partition retry budgets.
/// </summary>
internal static class WebhookProbeSchedule
{
    /// <summary>
    /// Samples a capped exponential delay with symmetric jitter.
    /// </summary>
    /// <param name="options">The validated timing options.</param>
    /// <param name="failures">The failed probe count.</param>
    /// <param name="sample">The jitter sample.</param>
    /// <param name="now">The current time.</param>
    /// <returns>The sampled absolute deadline.</returns>
    internal static DateTimeOffset NextDue(WebhookProbe options, int failures, double sample, DateTimeOffset now)
    {
        var seconds = Math.Min(options.MaximumDelaySeconds, options.InitialDelaySeconds * Math.Pow(options.BackoffFactor, Math.Min(failures, 1024)));
        var jitter = ((2 * sample) - 1) * options.JitterRatio;
        seconds = Math.Clamp(seconds * (1 + jitter), 0.001, options.MaximumDelaySeconds);
        var delay = TimeSpan.FromSeconds(seconds);
        return delay > DateTimeOffset.MaxValue - now ? DateTimeOffset.MaxValue : now + delay;
    }

    /// <summary>
    /// Advances the deadline while retaining any outstanding drain claim.
    /// </summary>
    /// <param name="control">The durable control.</param>
    /// <param name="due">The sampled deadline.</param>
    /// <param name="receiverDeadline">The receiver deadline, if any.</param>
    /// <returns>The next durable schedule.</returns>
    internal static WebhookProbeState Reschedule(WebhookProbeState control, DateTimeOffset due, DateTimeOffset? receiverDeadline)
    {
        if (receiverDeadline is { } deadline && deadline > due) due = deadline;
        var next = control with { Due = due, Failures = NextFailureCount(control) };
        return control.DrainPending ? next : ClearClaim(next);
    }

    /// <summary>
    /// Increments the independent probe counter without overflow.
    /// </summary>
    /// <param name="control">The current control.</param>
    /// <returns>The saturated failure count.</returns>
    internal static int NextFailureCount(WebhookProbeState control) => control.Failures < int.MaxValue ? control.Failures + 1 : int.MaxValue;

    /// <summary>
    /// Clears a claim after its subscriber acknowledges completion.
    /// </summary>
    /// <param name="control">The drained control.</param>
    /// <returns>The control without delivery claim metadata.</returns>
    internal static WebhookProbeState ClearClaim(WebhookProbeState control) =>
        control with { DrainPending = false, EpisodeId = null, Position = null, Partition = null, DefinitionFingerprint = null };
}
