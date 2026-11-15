// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.for_ReadModelPropertiesBuilder.when_targeting_a_dictionary_entry;

public class and_using_camel_case_naming : given.a_from_builder_with_dictionaries
{
    protected override INamingPolicy NamingPolicy => new CamelCaseNamingPolicy();

    void Because()
    {
        _builder
            .Set(m => m.Slots, e => e.Slot.Name).To(e => e.SeriesId)
            .RemoveKey(m => m.Counts, c => c.EventSourceId);
        _result = _builder.Build();
    }

    [Fact] void should_name_the_dictionary_and_event_key_by_the_policy() => _result.Properties["slots.$eventContent.slot.name"].ShouldEqual("seriesId");
    [Fact] void should_name_the_dictionary_and_context_key_by_the_policy() => _result.Properties["counts.$eventContext.eventSourceId"].ShouldEqual(WellKnownExpressions.Remove);
}
