// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.WireCompatibility.for_BaselineSelection.when_selecting;

public class and_the_ceiling_is_a_released_version : Specification
{
    BaselineSelection _result;

    void Because() => _result = BaselineSelection.From(["19.0.0", "19.1.0", "19.4.0", "19.6.1", "19.14.0"], null, "19.4.0", []);

    [Fact] void should_include_the_ceiling_itself() => _result.Baselines.SequenceEqual(["19.0.0", "19.1.0", "19.4.0"]).ShouldBeTrue();
}
