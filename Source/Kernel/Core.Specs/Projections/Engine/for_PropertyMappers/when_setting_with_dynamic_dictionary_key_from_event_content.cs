// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_PropertyMappers;

public class when_setting_with_dynamic_dictionary_key_from_event_content : Specification
{
    PropertyMapper<AppendedEvent, ExpandoObject> _propertyMapper;
    ExpandoObject _target;
    PropertyDifference _overwrite;

    void Establish()
    {
        _target = new();
        _propertyMapper = PropertyMappers.FromEventValueProvider(
            "slots.$eventContent.slot",
            EventValueProviders.EventContent("seriesId"));
    }

    void Because()
    {
        _propertyMapper(given.an_appended_event.with_slot("first", "series-1"), _target, ArrayIndexers.NoIndexers);
        _propertyMapper(given.an_appended_event.with_slot("second", "series-2"), _target, ArrayIndexers.NoIndexers);
        _overwrite = _propertyMapper(given.an_appended_event.with_slot("first", "series-3"), _target, ArrayIndexers.NoIndexers);
    }

    IDictionary<string, object> Slots => (IDictionary<string, object>)((dynamic)_target).slots;

    [Fact] void should_hold_one_key_per_slot() => Slots.Count.ShouldEqual(2);
    [Fact] void should_overwrite_the_value_under_an_existing_key() => Slots["first"].ShouldEqual("series-3");
    [Fact] void should_keep_the_value_under_another_key() => Slots["second"].ShouldEqual("series-2");
    [Fact] void should_report_the_resolved_key_in_the_difference() => _overwrite.PropertyPath.Path.ShouldEqual("slots.first");
    [Fact] void should_report_the_previous_value() => _overwrite.Original.ShouldEqual("series-1");
}
