// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Diagnostics;

namespace Cratis.Chronicle.XUnit.Integration.for_IntegrationTimings;

public class when_recording_nested_phases : Specification
{
    readonly List<string> _lines = [];
    long _clock;
    JsonElement _parent;
    JsonElement _child;
    JsonElement _summary;

    void Because()
    {
        var timings = new IntegrationTimings(_lines.Add, () => _clock, 1000, "fixture");
        using (var parent = timings.Begin("boot")!)
        {
            _clock = 10;
            using (var child = timings.Begin("connect")!)
            {
                child.Poll();
                child.Retry();
                _clock = 30;
                child.Success();
            }
            _clock = 50;
            parent.Success();
        }
        timings.Complete();
        _child = Parse(_lines[2]);
        _parent = Parse(_lines[3]);
        _summary = Parse(_lines[4]);
    }

    static JsonElement Parse(string line) => JsonSerializer.Deserialize<JsonElement>(line[(line.IndexOf(' ') + 1)..]);

    [Fact] void should_measure_elapsed_time_with_the_monotonic_clock() => _child.GetProperty("elapsedMs").GetDouble().ShouldEqual(20d);
    [Fact] void should_identify_the_parent() => _child.GetProperty("parent").GetInt64().ShouldEqual(_parent.GetProperty("sequence").GetInt64());
    [Fact] void should_keep_the_inclusive_parent_duration() => _parent.GetProperty("elapsedMs").GetDouble().ShouldEqual(50d);
    [Fact] void should_record_polls() => _child.GetProperty("polls").GetInt32().ShouldEqual(1);
    [Fact] void should_record_retries() => _child.GetProperty("retries").GetInt32().ShouldEqual(1);
    [Fact] void should_finish_the_instance() => _summary.GetProperty("completeness").GetString().ShouldEqual("complete");
}
