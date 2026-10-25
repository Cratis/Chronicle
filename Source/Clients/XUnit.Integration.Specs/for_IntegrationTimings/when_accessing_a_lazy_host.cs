// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Diagnostics;

namespace Cratis.Chronicle.XUnit.Integration.for_IntegrationTimings;

public class when_accessing_a_lazy_host : Specification
{
    readonly List<string> _lines = [];
    readonly object _factory = new();
    int _calls;
    string[] _phases;

    void Because()
    {
        var timings = new IntegrationTimings(_lines.Add, () => 0, 1000, "spec");
        IntegrationHostTimings.Measure(timings, _factory, false, () => ++_calls);
        IntegrationHostTimings.Measure(timings, _factory, false, () => ++_calls);
        IntegrationHostTimings.Measure(timings, new object(), true, () => ++_calls);
        _phases = _lines.Select(line => JsonSerializer.Deserialize<JsonElement>(line[(line.IndexOf(' ') + 1)..]))
            .Where(record => record.GetProperty("outcome").GetString() == "success")
            .Select(record => record.GetProperty("phase").GetString()!).ToArray();
    }

    [Fact] void should_not_add_accesses() => _calls.ShouldEqual(3);
    [Fact] void should_record_one_phase_per_access() => _phases.Length.ShouldEqual(3);
    [Fact] void should_record_the_first_shared_host_start() => _phases[0].ShouldEqual("host-silo-start-shared");
    [Fact] void should_distinguish_shared_host_reuse() => _phases[1].ShouldEqual("host-silo-reuse-shared");
    [Fact] void should_distinguish_a_context_host_start() => _phases[2].ShouldEqual("host-silo-start-per-context");
}
