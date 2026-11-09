// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_PropertyMappers;

public class when_setting_with_an_event_content_key_that_cannot_be_a_dictionary_key : Specification
{
    PropertyMapper<Concepts.Events.AppendedEvent, ExpandoObject> _propertyMapper;
    Exception _withDot;
    Exception _withDollar;
    Exception _withBracket;

    void Establish() => _propertyMapper = PropertyMappers.FromEventValueProvider(
        "slots.$eventContent.slot",
        EventValueProviders.EventContent("seriesId"));

    void Because()
    {
        _withDot = Catch.Exception(() => _propertyMapper(given.an_appended_event.with_slot("a.b", "series-1"), new(), ArrayIndexers.NoIndexers));
        _withDollar = Catch.Exception(() => _propertyMapper(given.an_appended_event.with_slot("$set", "series-1"), new(), ArrayIndexers.NoIndexers));
        _withBracket = Catch.Exception(() => _propertyMapper(given.an_appended_event.with_slot("[0]", "series-1"), new(), ArrayIndexers.NoIndexers));
    }

    [Fact] void should_refuse_a_key_with_a_dot() => _withDot.ShouldBeOfExactType<InvalidDictionaryKey>();
    [Fact] void should_refuse_a_key_starting_with_a_dollar() => _withDollar.ShouldBeOfExactType<InvalidDictionaryKey>();
    [Fact] void should_refuse_a_key_with_a_bracket() => _withBracket.ShouldBeOfExactType<InvalidDictionaryKey>();
}
