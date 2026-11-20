// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_Measurements;

public class when_committing_held_invocations : Specification
{
    const string Key = "spec/commit";
    OperationStats _stats = default!;

    void Establish()
    {
        Measurements.Clear();
        for (var index = 0; index < 5; index++)
        {
            Measurements.Hold(Key, new(1, 1, new(1, new Dictionary<string, long> { ["insert"] = 1 }), [TimeSpan.FromMilliseconds(1)], 0));
        }
    }

    void Because()
    {
        Measurements.Commit(2);
        _stats = Measurements.For(Key);
    }

    [Fact] void should_count_only_the_measured_iterations() => _stats.Operations.ShouldEqual(2);
    [Fact] void should_count_their_commands() => _stats.Commands.ShouldEqual(2);
}
