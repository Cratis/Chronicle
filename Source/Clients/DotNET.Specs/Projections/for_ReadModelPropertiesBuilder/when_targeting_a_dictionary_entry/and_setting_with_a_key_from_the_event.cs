// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_ReadModelPropertiesBuilder.when_targeting_a_dictionary_entry;

public class and_setting_with_a_key_from_the_event : given.a_from_builder_with_dictionaries
{
    void Because()
    {
        _builder.Set(m => m.Slots, e => e.Category).To(e => e.SeriesId);
        _result = _builder.Build();
    }

    [Fact] void should_target_the_entry_keyed_by_the_event_content() => _result.Properties.Keys.ShouldContainOnly("Slots.$eventContent.Category");
    [Fact] void should_set_the_entry_from_the_event_property() => _result.Properties["Slots.$eventContent.Category"].ShouldEqual("SeriesId");
}
