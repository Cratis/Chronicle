// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_ReadModelPropertiesBuilder.when_targeting_a_dictionary_entry;

public class and_the_entry_is_already_defined : given.a_from_builder_with_dictionaries
{
    Exception? _error;

    void Because()
    {
        _builder.Set(m => m.Slots, e => e.Category).To(e => e.SeriesId);
        _error = Catch.Exception(() => _builder.RemoveKey(m => m.Slots, e => e.Category));
    }

    [Fact] void should_fail_with_duplicate_property() => _error.ShouldBeOfExactType<DuplicatePropertyInProjection>();
}
