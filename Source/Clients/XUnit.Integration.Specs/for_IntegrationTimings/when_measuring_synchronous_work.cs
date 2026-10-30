// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Diagnostics;

namespace Cratis.Chronicle.XUnit.Integration.for_IntegrationTimings;

public class when_measuring_synchronous_work : Specification
{
    readonly List<string> _lines = [];
    readonly object _expected = new();
    readonly Exception _failure = new InvalidOperationException("Factory failure");
    object _result;
    Exception? _caught;
    int _calls;
    long _clock;
    JsonElement _record;

    void Because()
    {
        var timings = new IntegrationTimings(_lines.Add, () => _clock, 1000, "fixture");
        _result = timings.Measure("host-create", () =>
        {
            _calls++;
            _clock = 15;
            return _expected;
        });
        _caught = Catch.Exception(() => timings.Measure<object>("host-create", () => throw _failure));
        var line = _lines[1];
        _record = JsonSerializer.Deserialize<JsonElement>(line[(line.IndexOf(' ') + 1)..]);
    }

    [Fact] void should_construct_the_host_once() => _calls.ShouldEqual(1);
    [Fact] void should_return_the_original_host() => _result.ShouldBeSame(_expected);
    [Fact] void should_measure_lazy_startup() => _record.GetProperty("elapsedMs").GetDouble().ShouldEqual(15d);
    [Fact] void should_preserve_the_original_factory_exception() => _caught.ShouldBeSame(_failure);
    [Fact] void should_restore_the_ambient_scope() => IntegrationTimings.CurrentPhase.ShouldBeNull();
}
