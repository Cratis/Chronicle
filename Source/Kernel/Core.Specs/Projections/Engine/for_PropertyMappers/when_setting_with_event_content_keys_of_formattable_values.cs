// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_PropertyMappers;

public class when_setting_with_event_content_keys_of_formattable_values : Specification
{
    static readonly DateTimeOffset _occurred = new(2026, 3, 4, 5, 6, 7, TimeSpan.FromHours(2));

    PropertyMapper<AppendedEvent, ExpandoObject> _propertyMapper;
    ExpandoObject _target;

    void Establish()
    {
        _target = new();
        _propertyMapper = PropertyMappers.FromEventValueProvider("slots.$eventContent.slot", EventValueProviders.EventContent("seriesId"));
    }

    void Because()
    {
        _propertyMapper(given.an_appended_event.with_content(Content(-5)), _target, ArrayIndexers.NoIndexers);
        _propertyMapper(given.an_appended_event.with_content(Content(_occurred)), _target, ArrayIndexers.NoIndexers);
    }

    IDictionary<string, object?> Slots => (IDictionary<string, object?>)((IDictionary<string, object?>)_target)["slots"]!;

    [Fact] void should_format_a_number_with_the_invariant_culture() => Slots.ContainsKey("-5").ShouldBeTrue();
    [Fact] void should_format_a_date_as_iso_8601_with_a_comma_as_the_decimal_sign() => Slots.ContainsKey("2026-03-04T05:06:07,0000000+02:00").ShouldBeTrue();

    static ExpandoObject Content(object slot)
    {
        var content = new ExpandoObject();
        ((IDictionary<string, object?>)content)["slot"] = slot;
        ((IDictionary<string, object?>)content)["seriesId"] = "series";
        return content;
    }
}
