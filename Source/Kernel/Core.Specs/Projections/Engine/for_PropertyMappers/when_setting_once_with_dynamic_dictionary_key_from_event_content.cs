// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_PropertyMappers;

public class when_setting_once_with_dynamic_dictionary_key_from_event_content : Specification
{
    PropertyMapper<AppendedEvent, ExpandoObject> _propertyMapper;
    ExpandoObject _target;
    PropertyDifference _repeated;

    void Establish()
    {
        _target = new();
        _propertyMapper = PropertyMappers.SetOnceFromEventValueProvider("firstSeries.$eventContent.slot", EventValueProviders.EventContent("seriesId"));
    }

    void Because()
    {
        _propertyMapper(given.an_appended_event.with_slot("first", "series-1"), _target, ArrayIndexers.NoIndexers);
        _repeated = _propertyMapper(given.an_appended_event.with_slot("first", "series-2"), _target, ArrayIndexers.NoIndexers);
        _propertyMapper(given.an_appended_event.with_slot("second", "series-3"), _target, ArrayIndexers.NoIndexers);
    }

    IDictionary<string, object> FirstSeries => (IDictionary<string, object>)((dynamic)_target).firstSeries;

    [Fact] void should_keep_the_first_value_under_a_key() => FirstSeries["first"].ShouldEqual("series-1");
    [Fact] void should_assign_a_new_key() => FirstSeries["second"].ShouldEqual("series-3");
    [Fact] void should_not_report_the_repeated_assignment() => _repeated.HasChanges().ShouldBeFalse();
}
