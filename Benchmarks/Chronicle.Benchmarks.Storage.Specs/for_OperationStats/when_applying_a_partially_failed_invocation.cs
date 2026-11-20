// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_OperationStats;

public class when_applying_a_partially_failed_invocation : Specification
{
    OperationStats _stats = default!;

    void Because()
    {
        _stats = new();
        _stats.Apply(new(3, 3, new(6, new Dictionary<string, long> { ["insert"] = 6 }), [TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(2), TimeSpan.FromMilliseconds(3)], 1));
    }

    [Fact] void should_count_only_the_successful_operations() => _stats.Operations.ShouldEqual(3);
    [Fact] void should_count_only_the_successful_events() => _stats.Events.ShouldEqual(3);
    [Fact] void should_count_the_failure() => _stats.Errors.ShouldEqual(1);
}
