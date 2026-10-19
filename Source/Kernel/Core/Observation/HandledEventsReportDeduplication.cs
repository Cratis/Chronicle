// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Recognizes a handled-events report that has already been taken, so a job step can send it again after a timeout.
/// </summary>
/// <remarks>
/// <para>
/// A grain call that times out may still have run: the caller cannot tell a report that never arrived from one whose
/// answer did not come back in time. Sending it again without recognizing it would count the events twice; not
/// sending it would leave them uncounted. A step sends the reports for a partition one after another with
/// increasing <see cref="EventSequenceNumber"/>s, so the last one taken from each step for each partition is enough to
/// recognize a repeat.
/// </para>
/// <para>
/// Orleans drops a request whose response timeout has passed before it runs, so every attempt of a report runs, if at
/// all, within one response timeout of being sent. A step makes at most <see cref="TransientTimeouts.MaximumAttempts"/>
/// attempts, each sent once the previous one has failed - which happens within one response timeout - and the retry
/// delay has passed. Every attempt that runs therefore runs within
/// <see cref="TransientTimeouts.MaximumAttempts"/> × (response timeout + retry delay) of the first one taken, and
/// <see cref="WindowFor"/> is never shorter than that. What a step last reported for a partition is remembered for
/// <see cref="Window"/> after it was last seen, which keeps the memory to the steps that are reporting rather than
/// every step that ever did, without forgetting a report while it can still arrive again.
/// </para>
/// </remarks>
internal sealed class HandledEventsReportDeduplication
{
    /// <summary>
    /// The default for how long what a step last reported is remembered.
    /// </summary>
    public static readonly TimeSpan DefaultWindow = TimeSpan.FromMinutes(15);

    readonly Dictionary<(JobStepId Step, Key Partition), LastReport> _lastReports = [];

    /// <summary>
    /// Gets or sets how long what a step last reported is remembered after it was last seen.
    /// </summary>
    public TimeSpan Window { get; set; } = DefaultWindow;

    /// <summary>
    /// Gets the number of steps and partitions whose last report is remembered.
    /// </summary>
    public int Count => _lastReports.Count;

    /// <summary>
    /// Gets how long what a step last reported has to be remembered for a repeat to be recognized, given the response
    /// timeout reports are sent with.
    /// </summary>
    /// <param name="responseTimeout">The response timeout a report is sent with.</param>
    /// <returns>The longer of <see cref="DefaultWindow"/> and the span within which every attempt of a report runs.</returns>
    public static TimeSpan WindowFor(TimeSpan responseTimeout)
    {
        var retrySpan = (responseTimeout + TransientTimeouts.DefaultRetryDelay) * TransientTimeouts.MaximumAttempts;
        return retrySpan > DefaultWindow ? retrySpan + responseTimeout : DefaultWindow;
    }

    /// <summary>
    /// Takes a report unless it is one the step already sent.
    /// </summary>
    /// <param name="reportedBy">The <see cref="JobStepId"/> of the reporting step.</param>
    /// <param name="partition">The partition the report is for.</param>
    /// <param name="handledUpTo">The <see cref="EventSequenceNumber"/> of the last event the report covers.</param>
    /// <returns>True if the report is new and its counts are to be taken; false if it repeats one already taken.</returns>
    public bool TryTake(JobStepId reportedBy, Key partition, EventSequenceNumber handledUpTo)
    {
        if (!handledUpTo.IsActualValue)
        {
            return true;
        }

        var key = (reportedBy, partition);
        var now = Stopwatch.GetTimestamp();
        if (_lastReports.TryGetValue(key, out var last) && handledUpTo <= last.HandledUpTo)
        {
            _lastReports[key] = last with { SeenAt = now };
            return false;
        }

        _lastReports[key] = new(handledUpTo, now);
        return true;
    }

    /// <summary>
    /// Forgets the steps that have not reported within <see cref="Window"/>.
    /// </summary>
    public void ForgetQuietSteps()
    {
        foreach (var (key, last) in _lastReports)
        {
            if (Stopwatch.GetElapsedTime(last.SeenAt) >= Window)
            {
                _lastReports.Remove(key);
            }
        }
    }

    record struct LastReport(EventSequenceNumber HandledUpTo, long SeenAt);
}
