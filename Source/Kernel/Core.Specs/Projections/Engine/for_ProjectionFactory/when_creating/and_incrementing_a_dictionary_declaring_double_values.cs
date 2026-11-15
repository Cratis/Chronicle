// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_incrementing_a_dictionary_declaring_double_values : given.a_typed_dictionary_projection
{
    object? _result;

    async Task Because() => _result = await Project("amounts.$eventContent.slot", WellKnownExpressions.Increment);

    [Fact] void should_increment_as_int64_as_it_always_has() => _result.ShouldBeOfExactType<long>();
}
