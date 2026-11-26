// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.ReadModels;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.given;

namespace Cratis.Chronicle.Testing.Assertions.for_ExpectedValueComparer;

public class when_matching_cyclic_read_models : Specification
{
    AssertionDocument _expected;
    AssertionDocument _actual;
    ValueDifference? _match;
    ValueDifference? _mismatch;

    void Establish()
    {
        _expected = new("Ada", 12m, [1, 2], new Dictionary<string, JoinCustomerId?>());
        _actual = new("Ada", 12m, [1, 2], new Dictionary<string, JoinCustomerId?>());
        _expected.Next = _expected;
        _actual.Next = _actual;
    }

    void Because()
    {
        _match = ExpectedValueComparer.Compare(_expected, _actual, ValueComparison.Exact);
        _mismatch = ExpectedValueComparer.Compare(_expected with { Name = "Other" }, _actual, ValueComparison.Exact);
    }

    [Fact] void should_terminate_for_equivalent_cycles() => _match.ShouldBeNull();
    [Fact] void should_still_detect_values_that_differ_in_a_cycle() => _mismatch!.Path.ShouldEqual("$.Name");
}
