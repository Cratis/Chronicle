// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_PropertyMappers;

public class when_setting_with_event_content_keys_of_temporal_and_concept_values : Specification
{
    PropertyMapper<AppendedEvent, ExpandoObject> _propertyMapper;
    ExpandoObject _target;

    void Establish()
    {
        _target = new();
        _propertyMapper = PropertyMappers.FromEventValueProvider("slots.$eventContent.slot", EventValueProviders.EventContent("seriesId"));
    }

    void Because()
    {
        _propertyMapper(given.an_appended_event.with_content(Content(new DateTime(2026, 3, 4, 5, 6, 7, DateTimeKind.Utc))), _target, ArrayIndexers.NoIndexers);
        _propertyMapper(given.an_appended_event.with_content(Content(new DateOnly(2026, 3, 4))), _target, ArrayIndexers.NoIndexers);
        _propertyMapper(given.an_appended_event.with_content(Content(new TimeOnly(5, 6, 7))), _target, ArrayIndexers.NoIndexers);
        _propertyMapper(given.an_appended_event.with_content(Content(new given.a_slot_name("morning"))), _target, ArrayIndexers.NoIndexers);
        _propertyMapper(given.an_appended_event.with_content(Content(true)), _target, ArrayIndexers.NoIndexers);
    }

    IDictionary<string, object?> Slots => (IDictionary<string, object?>)((IDictionary<string, object?>)_target)["slots"]!;

    [Fact] void should_format_a_date_time_as_iso_8601_with_a_comma_as_the_decimal_sign() => Slots.ContainsKey("2026-03-04T05:06:07,0000000Z").ShouldBeTrue();
    [Fact] void should_format_a_date_only_as_iso_8601() => Slots.ContainsKey("2026-03-04").ShouldBeTrue();
    [Fact] void should_format_a_time_only_with_a_comma_as_the_decimal_sign() => Slots.ContainsKey("05:06:07,0000000").ShouldBeTrue();
    [Fact] void should_use_the_value_of_a_concept() => Slots.ContainsKey("morning").ShouldBeTrue();
    [Fact] void should_use_the_string_form_of_other_values() => Slots.ContainsKey("True").ShouldBeTrue();

    static ExpandoObject Content(object slot)
    {
        var content = new ExpandoObject();
        ((IDictionary<string, object?>)content)["slot"] = slot;
        ((IDictionary<string, object?>)content)["seriesId"] = "series";
        return content;
    }
}
