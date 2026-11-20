// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_OperationStats;

public class when_recording_operations : Specification
{
    OperationStats _stats = default!;

    void Because()
    {
        _stats = new();
        _stats.Record(10, new(4, new Dictionary<string, long> { ["insert"] = 4 }), TimeSpan.FromMilliseconds(5));
        _stats.Record(10, new(2, new Dictionary<string, long> { ["insert"] = 2 }), TimeSpan.FromMilliseconds(7));
    }

    [Fact] void should_count_operations() => _stats.Operations.ShouldEqual(2);
    [Fact] void should_count_commands() => _stats.Commands.ShouldEqual(6);
    [Fact] void should_compute_commands_per_operation() => _stats.CommandsPerOperation.ShouldEqual(3.0);
    [Fact] void should_compute_commands_per_thousand_events() => _stats.CommandsPer1000Events.ShouldEqual(300.0);
}
