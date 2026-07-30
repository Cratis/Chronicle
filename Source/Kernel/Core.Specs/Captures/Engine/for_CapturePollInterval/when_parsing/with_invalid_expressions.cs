// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.Engine.for_CapturePollInterval.when_parsing;

public class with_invalid_expressions : Specification
{
    [Fact] void should_not_parse_empty() => CapturePollInterval.TryParse(string.Empty, out _).ShouldBeFalse();
    [Fact] void should_not_parse_null() => CapturePollInterval.TryParse(null, out _).ShouldBeFalse();
    [Fact] void should_not_parse_unknown_unit() => CapturePollInterval.TryParse("5x", out _).ShouldBeFalse();
    [Fact] void should_not_parse_non_numeric() => CapturePollInterval.TryParse("often", out _).ShouldBeFalse();
    [Fact] void should_not_parse_zero() => CapturePollInterval.TryParse("0m", out _).ShouldBeFalse();
}
