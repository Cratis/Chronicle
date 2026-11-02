// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry;

/// <summary>
/// Keeps trace context flowing through Orleans without exporting Orleans activities.
/// </summary>
public sealed class OrleansTraceContextPropagation : IHostedService, IDisposable
{
    [ThreadStatic]
    static ParentFlags _parentFlags;

    ActivityListener? _listener;

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_listener is not null) return Task.CompletedTask;

        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name.Equals(OrleansActivitySources.Runtime, StringComparison.Ordinal) || source.Name.Equals(OrleansActivitySources.Application, StringComparison.Ordinal),
            Sample = Sample,
            ActivityStarted = RestoreRecordedFlag
        };
        ActivitySource.AddActivityListener(_listener);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        Dispose();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _listener?.Dispose();
        _listener = null;
    }

    /// <summary>
    /// Restores the Recorded trace flag that ActivitySource clears for propagation-only sampling, so injected context and
    /// downstream ParentBased sampling still see a sampled caller. The shared all-data flag is never touched, so other
    /// listeners on the Orleans sources keep their own all-data requests.
    /// </summary>
    /// <param name="activity">The started carrier <see cref="Activity"/>.</param>
    static void RestoreRecordedFlag(Activity activity)
    {
        if (_parentFlags.HasRecorded && _parentFlags.SpanId == activity.ParentSpanId)
        {
            activity.ActivityTraceFlags |= ActivityTraceFlags.Recorded;
        }

        _parentFlags = default;
    }

    static ActivitySamplingResult Sample(ref ActivityCreationOptions<ActivityContext> options)
    {
        var parent = options.Parent;

        // Orleans 10.4 ActivityPropagationGrainCallFilter only injects context if StartActivity succeeds.
        // Do not create root activities for system calls. PropagationData requests no all-data, so carriers stay
        // lightweight; the parent's Recorded flag is remembered for ActivityStarted on this thread. OpenTelemetry does
        // not listen to these sources, so these carrier activities are not exported.
        if (parent.TraceId == default) return ActivitySamplingResult.None;

        _parentFlags = new(parent.SpanId, parent.TraceFlags.HasFlag(ActivityTraceFlags.Recorded));
        return ActivitySamplingResult.PropagationData;
    }

    readonly record struct ParentFlags(ActivitySpanId SpanId, bool HasRecorded);
}
