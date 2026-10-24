// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Diagnostics;

namespace Cratis.Chronicle.XUnit.Integration.for_IntegrationTimings;

public class when_a_phase_is_interrupted : Specification
{
    readonly List<string> _lines = [];
    JsonElement _summary;

    void Because()
    {
        var timings = new IntegrationTimings(_lines.Add, () => 10, 1000, "fixture");
        using var phase = timings.Begin("boot");
        timings.Complete();
        timings.Complete();
        var line = _lines[1];
        _summary = JsonSerializer.Deserialize<JsonElement>(line[(line.IndexOf(' ') + 1)..]);
    }

    [Fact] void should_mark_the_summary_as_partial() => _summary.GetProperty("completeness").GetString().ShouldEqual("partial");
    [Fact] void should_write_only_one_summary() => _lines.Count(_ => _.StartsWith("CHRONICLE_INTEGRATION_SUMMARY ")).ShouldEqual(1);
}
