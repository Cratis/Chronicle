// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.for_PropertyMappers;

public class when_accumulating_with_dynamic_dictionary_key_from_event_content : Specification
{
    static readonly JsonSchemaProperty _int64 = new() { Type = JsonObjectType.Integer, Format = "int64" };

    PropertyMapper<AppendedEvent, ExpandoObject> _count;
    PropertyMapper<AppendedEvent, ExpandoObject> _increment;
    PropertyMapper<AppendedEvent, ExpandoObject> _decrement;
    PropertyMapper<AppendedEvent, ExpandoObject> _add;
    PropertyMapper<AppendedEvent, ExpandoObject> _subtract;
    ExpandoObject _target;

    void Establish()
    {
        var typeFormats = new TypeFormats();
        _target = new();
        _count = PropertyMappers.Count(typeFormats, "counts.$eventContent.slot", _int64);
        _increment = PropertyMappers.Increment(typeFormats, "increments.$eventContent.slot", _int64);
        _decrement = PropertyMappers.Decrement(typeFormats, "decrements.$eventContent.slot", _int64);
        _add = PropertyMappers.AddWithEventValueProvider(typeFormats, "sums.$eventContent.slot", _int64, EventValueProviders.EventContent("seriesId"));
        _subtract = PropertyMappers.SubtractWithEventValueProvider(typeFormats, "differences.$eventContent.slot", _int64, EventValueProviders.EventContent("seriesId"));
    }

    void Because()
    {
        foreach (var (slot, amount) in new[] { ("first", 2L), ("first", 3L), ("second", 7L) })
        {
            var @event = given.an_appended_event.with_slot(slot, amount);
            _count(@event, _target, ArrayIndexers.NoIndexers);
            _increment(@event, _target, ArrayIndexers.NoIndexers);
            _decrement(@event, _target, ArrayIndexers.NoIndexers);
            _add(@event, _target, ArrayIndexers.NoIndexers);
            _subtract(@event, _target, ArrayIndexers.NoIndexers);
        }
    }

    long ValueOf(string dictionary, string key) => (long)((IDictionary<string, object>)((IDictionary<string, object?>)_target)[dictionary])[key];

    [Fact] void should_count_per_key() => ValueOf("counts", "first").ShouldEqual(2);
    [Fact] void should_increment_per_key() => ValueOf("increments", "second").ShouldEqual(1);
    [Fact] void should_decrement_per_key() => ValueOf("decrements", "first").ShouldEqual(-2);
    [Fact] void should_add_per_key() => ValueOf("sums", "first").ShouldEqual(5);
    [Fact] void should_subtract_per_key() => ValueOf("differences", "second").ShouldEqual(-7);
}
