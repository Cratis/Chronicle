// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Json.for_ExpandoObjectConverter.when_converting_non_compliant_string_scalars;

public class and_it_targets_an_integer : given.a_converter_with_scalar_schema
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => Convert("count", "42"));

    [Fact] void should_reject_the_string() => _error.ShouldBeOfExactType<InvalidOperationException>();
}
