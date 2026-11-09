// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_counting_by_event_type_into_a_dictionary_declaring_int32_values : given.a_typed_dictionary_projection
{
    object? _result;

    async Task Because() => _result = await Project("counts.$eventContext.eventType.id", WellKnownExpressions.Count, _amountRecorded.Id.Value);

    [Fact] void should_count_as_int64_as_it_always_has() => _result.ShouldBeOfExactType<long>();
}
