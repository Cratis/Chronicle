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
    ActivityListener? _listener;

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_listener is not null) return Task.CompletedTask;

        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name.Equals(OrleansActivitySources.Runtime, StringComparison.Ordinal) || source.Name.Equals(OrleansActivitySources.Application, StringComparison.Ordinal),
            Sample = Sample,
            ActivityStarted = DoNotRequestAllData
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
    /// Withdraws the all-data request ActivitySource sets for AllDataAndRecorded, so Orleans' grain call filter does not
    /// format grain ids and exception strings for carriers that are never exported. The Recorded flag lives on the
    /// trace flags, so propagation and downstream sampling are unaffected.
    /// </summary>
    /// <param name="activity">The started carrier <see cref="Activity"/>.</param>
    static void DoNotRequestAllData(Activity activity) => activity.IsAllDataRequested = false;

    static ActivitySamplingResult Sample(ref ActivityCreationOptions<ActivityContext> options)
    {
        var parent = options.Parent;

        // Orleans 10.4 ActivityPropagationGrainCallFilter only injects context if StartActivity succeeds.
        // Do not create root activities for system calls. For sampled parents, AllDataAndRecorded is
        // required: ActivitySource otherwise clears Recorded, and ParentBased drops callee kernel spans.
        // All-data is withdrawn again in ActivityStarted. OpenTelemetry does not listen to these sources,
        // so these carrier activities are not exported.
        if (parent.TraceId == default) return ActivitySamplingResult.None;

        return parent.TraceFlags.HasFlag(ActivityTraceFlags.Recorded)
            ? ActivitySamplingResult.AllDataAndRecorded
            : ActivitySamplingResult.PropagationData;
    }
}
