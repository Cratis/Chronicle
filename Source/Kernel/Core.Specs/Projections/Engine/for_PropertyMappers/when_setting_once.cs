// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_PropertyMappers;

public class when_setting_once : Specification
{
    PropertyMapper<AppendedEvent, ExpandoObject> _propertyMapper;
    ExpandoObject _target;
    PropertyDifference _first;
    PropertyDifference _second;

    void Establish()
    {
        _target = new();
        _propertyMapper = PropertyMappers.SetOnceFromEventValueProvider("createdBy", EventValueProviders.EventContent("seriesId"));
    }

    void Because()
    {
        _first = _propertyMapper(given.an_appended_event.with_slot("first", "series-1"), _target, ArrayIndexers.NoIndexers);
        _second = _propertyMapper(given.an_appended_event.with_slot("first", "series-2"), _target, ArrayIndexers.NoIndexers);
    }

    [Fact] void should_assign_the_first_value() => ((IDictionary<string, object?>)_target)["createdBy"].ShouldEqual("series-1");
    [Fact] void should_report_the_first_assignment() => _first.HasChanges().ShouldBeTrue();
    [Fact] void should_not_report_the_second_assignment() => _second.HasChanges().ShouldBeFalse();
}
