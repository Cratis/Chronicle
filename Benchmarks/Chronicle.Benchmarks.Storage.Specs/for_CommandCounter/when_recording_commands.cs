// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_CommandCounter;

public class when_recording_commands : Specification
{
    CommandSnapshot _before = default!;
    CommandSnapshot _delta = default!;
    CommandCounter _counter = default!;

    void Establish()
    {
        _counter = new();
        _counter.Record("insert");
        _before = _counter.Snapshot();
    }

    void Because()
    {
        _counter.Record("insert");
        _counter.Record("find");
        _counter.Record("hello");
        _counter.Record("endSessions");
        _delta = _counter.Snapshot().Since(_before);
    }

    [Fact] void should_count_storage_commands() => _delta.Total.ShouldEqual(2);
    [Fact] void should_count_per_type() => _delta.ByType["insert"].ShouldEqual(1);
    [Fact] void should_ignore_housekeeping() => _delta.ByType.ContainsKey("hello").ShouldBeFalse();
    [Fact] void should_not_report_unchanged_types() => _delta.ByType.Count.ShouldEqual(2);
}
