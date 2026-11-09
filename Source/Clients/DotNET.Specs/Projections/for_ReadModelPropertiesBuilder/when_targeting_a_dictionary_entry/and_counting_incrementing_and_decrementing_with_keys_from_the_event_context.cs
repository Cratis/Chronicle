// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_ReadModelPropertiesBuilder.when_targeting_a_dictionary_entry;

public class and_counting_incrementing_and_decrementing_with_keys_from_the_event_context : given.a_from_builder_with_dictionaries
{
    void Because()
    {
        _builder
            .Count(m => m.Counts, c => c.EventSourceId)
            .Increment(m => m.Counts, c => c.Namespace)
            .Decrement(m => m.Counts, c => c.CorrelationId);
        _result = _builder.Build();
    }

    [Fact] void should_count_into_the_entry_keyed_by_the_event_source_id() => _result.Properties["Counts.$eventContext.EventSourceId"].ShouldEqual(WellKnownExpressions.Count);
    [Fact] void should_increment_the_entry_keyed_by_the_namespace() => _result.Properties["Counts.$eventContext.Namespace"].ShouldEqual(WellKnownExpressions.Increment);
    [Fact] void should_decrement_the_entry_keyed_by_the_correlation_id() => _result.Properties["Counts.$eventContext.CorrelationId"].ShouldEqual(WellKnownExpressions.Decrement);
}
