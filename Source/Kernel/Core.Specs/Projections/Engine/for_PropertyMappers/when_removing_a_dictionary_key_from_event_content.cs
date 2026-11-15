// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_PropertyMappers;

public class when_removing_a_dictionary_key_from_event_content : Specification
{
    PropertyMapper<AppendedEvent, ExpandoObject> _propertyMapper;
    ExpandoObject _target;
    PropertyDifference _result;

    void Establish()
    {
        _target = new();
        var slots = new ExpandoObject();
        ((IDictionary<string, object?>)slots)["first"] = "series-1";
        ((IDictionary<string, object?>)slots)["second"] = "series-2";
        ((IDictionary<string, object?>)_target)["slots"] = slots;
        _propertyMapper = PropertyMappers.Remove("slots.$eventContent.slot");
    }

    void Because() => _result = _propertyMapper(given.an_appended_event.with_slot("first"), _target, ArrayIndexers.NoIndexers);

    IDictionary<string, object> Slots => (IDictionary<string, object>)((dynamic)_target).slots;

    [Fact] void should_remove_the_key() => Slots.ContainsKey("first").ShouldBeFalse();
    [Fact] void should_keep_the_other_key() => Slots["second"].ShouldEqual("series-2");
    [Fact] void should_report_a_removal() => _result.IsRemoval.ShouldBeTrue();
    [Fact] void should_report_changes() => _result.HasChanges().ShouldBeTrue();
    [Fact] void should_report_the_resolved_key() => _result.PropertyPath.Path.ShouldEqual("slots.first");
    [Fact] void should_report_the_removed_value() => _result.Original.ShouldEqual("series-1");
}
