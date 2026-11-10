// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_ReadModelPropertiesBuilder.when_targeting_a_dictionary_entry;

public class and_counting_incrementing_and_decrementing_with_keys_from_the_event : given.a_from_builder_with_dictionaries
{
    void Because()
    {
        _builder
            .Count(m => m.Counts, e => e.Category)
            .Increment(m => m.Counts, e => e.Name)
            .Decrement(m => m.Counts, e => e.SeriesId);
        _result = _builder.Build();
    }

    [Fact] void should_count_into_the_entry_keyed_by_the_category() => _result.Properties["Counts.$eventContent.Category"].ShouldEqual(WellKnownExpressions.Count);
    [Fact] void should_increment_the_entry_keyed_by_the_name() => _result.Properties["Counts.$eventContent.Name"].ShouldEqual(WellKnownExpressions.Increment);
    [Fact] void should_decrement_the_entry_keyed_by_the_series() => _result.Properties["Counts.$eventContent.SeriesId"].ShouldEqual(WellKnownExpressions.Decrement);
}
