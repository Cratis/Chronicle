// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Diagnostics;

namespace Cratis.Chronicle.XUnit.Integration.for_IntegrationTimings;

public class when_a_lazy_host_access_fails : Specification
{
    readonly List<string> _lines = [];
    readonly Exception _original = new InvalidOperationException("host failed");
    Exception? _caught;
    int _calls;
    JsonElement[] _records;

    void Because()
    {
        var factory = new object();
        var timings = new IntegrationTimings(_lines.Add, () => 0, 1000, "spec");
        try
        {
            IntegrationHostTimings.Measure<int>(timings, factory, true, () =>
            {
                _calls++;
                throw _original;
            });
        }
        catch (Exception exception) { _caught = exception; }
        IntegrationHostTimings.Measure(timings, factory, true, () => ++_calls);
        _records = _lines.Select(line => JsonSerializer.Deserialize<JsonElement>(line[(line.IndexOf(' ') + 1)..]))
            .Where(record => record.GetProperty("outcome").GetString() != "started").ToArray();
    }

    [Fact] void should_preserve_the_exception() => _caught.ShouldEqual(_original);
    [Fact] void should_not_add_a_retry() => _calls.ShouldEqual(2);
    [Fact] void should_record_failure() => _records[0].GetProperty("outcome").GetString().ShouldEqual("failure");
    [Fact] void should_not_label_the_next_attempt_as_a_reused_host() => _records[1].GetProperty("phase").GetString().ShouldEqual("host-silo-start-per-context");
}
