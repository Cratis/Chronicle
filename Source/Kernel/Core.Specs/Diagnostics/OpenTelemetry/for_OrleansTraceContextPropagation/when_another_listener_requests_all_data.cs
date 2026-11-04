// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.given;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.for_OrleansTraceContextPropagation;

[Collection(nameof(orleans_activity_source_collection))]
public class when_another_listener_requests_all_data : Specification
{
    OrleansTraceContextPropagation _propagation;
    ActivitySource _source;
    ActivityListener _other;
    Activity _caller;
    Activity _root;
    Activity _parented;
    bool _rootEnriched;
    bool _parentedEnriched;

    async Task Establish()
    {
        _source = new(OrleansActivitySources.Application);
        _propagation = new();
        await _propagation.StartAsync(CancellationToken.None);
        _other = new ActivityListener
        {
            ShouldListenTo = source => source.Name == OrleansActivitySources.Application,
            Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(_other);
    }

    void Because()
    {
        _root = _source.StartActivity("root");
        _rootEnriched = Enrich(_root);
        _root?.Stop();

        _caller = new Activity("caller")
            .SetParentId(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded).Start();
        _parented = _source.StartActivity("parented");
        _parentedEnriched = Enrich(_parented);
        _parented?.Stop();
    }

    async Task Destroy()
    {
        _caller?.Dispose();
        _other.Dispose();
        await _propagation.StopAsync(CancellationToken.None);
        _propagation.Dispose();
        _source.Dispose();
    }

    [Fact] void should_keep_all_data_on_the_root_activity() => _root.IsAllDataRequested.ShouldBeTrue();
    [Fact] void should_keep_enrichment_on_the_root_activity() => _rootEnriched.ShouldBeTrue();
    [Fact] void should_keep_all_data_on_the_parented_activity() => _parented.IsAllDataRequested.ShouldBeTrue();
    [Fact] void should_keep_enrichment_on_the_parented_activity() => _parentedEnriched.ShouldBeTrue();
    [Fact] void should_keep_the_recorded_flag_on_the_parented_activity() => _parented.Recorded.ShouldBeTrue();

    static bool Enrich(Activity? activity)
    {
        if (activity?.IsAllDataRequested != true) return false;
        activity.SetTag("enriched", "yes");
        return string.Equals(activity.GetTagItem("enriched") as string, "yes", StringComparison.Ordinal);
    }
}
