// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

public class when_constructing_unrepresentable_values : Specification
{
    Exception _protocol;
    Exception _separator;
    Exception _comma;
    void Because()
    {
        _protocol = Catch.Exception(() => _ = new GenerationProtocolVersion(uint.MaxValue));
        _separator = Catch.Exception(() => _ = new GenerationCapability("invalid\u001fvalue"));
        _comma = Catch.Exception(() => _ = new GenerationCapability("invalid,value"));
    }
    [Fact] void should_refuse_protocols_not_representable_in_sql() => _protocol.ShouldBeOfExactType<ArgumentOutOfRangeException>();
    [Fact] void should_refuse_unit_separators() => _separator.ShouldBeOfExactType<ArgumentException>();
    [Fact] void should_refuse_capability_delimiters() => _comma.ShouldBeOfExactType<ArgumentException>();
}
