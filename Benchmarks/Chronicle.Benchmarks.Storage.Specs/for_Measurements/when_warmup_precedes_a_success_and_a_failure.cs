// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_Measurements;

public class when_warmup_precedes_a_success_and_a_failure : Specification
{
    const string Key = "spec/warmup";
    OperationStats _stats = default!;

    void Establish() => Measurements.Clear();

    void Because()
    {
        var success = new InvocationRecord(1, 1, new(1, new Dictionary<string, long> { ["insert"] = 1 }), [TimeSpan.FromMilliseconds(1)], 0);
        Measurements.Record(Key, success);
        using (Measurements.Counting())
        {
            Measurements.Record(Key, success);
            Measurements.Record(Key, InvocationRecord.Failure);
        }

        Measurements.Record(Key, success);
        _stats = Measurements.For(Key);
    }

    [Fact] void should_count_only_the_counted_success() => _stats.Operations.ShouldEqual(1);
    [Fact] void should_count_only_the_counted_failure() => _stats.Errors.ShouldEqual(1);
    [Fact] void should_count_only_the_counted_commands() => _stats.Commands.ShouldEqual(1);
}
