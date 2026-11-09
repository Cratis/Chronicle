// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_PropertyMappers;

public class when_setting_once_after_the_value_was_reset_to_null : Specification
{
    PropertyMapper<AppendedEvent, ExpandoObject> _propertyMapper;
    ExpandoObject _target;
    PropertyDifference _result;

    void Establish()
    {
        _target = new();
        ((IDictionary<string, object?>)_target)["createdBy"] = null;
        _propertyMapper = PropertyMappers.SetOnceFromEventValueProvider("createdBy", EventValueProviders.EventContent("seriesId"));
    }

    void Because() => _result = _propertyMapper(given.an_appended_event.with_slot("first", "series-1"), _target, ArrayIndexers.NoIndexers);

    [Fact] void should_assign_the_value() => ((IDictionary<string, object?>)_target)["createdBy"].ShouldEqual("series-1");
    [Fact] void should_report_the_assignment() => _result.HasChanges().ShouldBeTrue();
}
