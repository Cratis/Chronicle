// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_PropertyMappers;

public class when_removing_a_dictionary_key_that_is_not_present : Specification
{
    PropertyMapper<AppendedEvent, ExpandoObject> _propertyMapper;
    ExpandoObject _target;
    PropertyDifference _missingKey;
    PropertyDifference _missingDictionary;

    void Establish()
    {
        _target = new();
        _propertyMapper = PropertyMappers.Remove("slots.$eventContent.slot");
    }

    void Because()
    {
        _missingDictionary = _propertyMapper(given.an_appended_event.with_slot("first"), _target, ArrayIndexers.NoIndexers);
        ((IDictionary<string, object?>)_target)["slots"] = new ExpandoObject();
        _missingKey = _propertyMapper(given.an_appended_event.with_slot("first"), _target, ArrayIndexers.NoIndexers);
    }

    [Fact] void should_not_change_anything_when_the_dictionary_is_missing() => _missingDictionary.HasChanges().ShouldBeFalse();
    [Fact] void should_not_change_anything_when_the_key_is_missing() => _missingKey.HasChanges().ShouldBeFalse();
    [Fact] void should_not_report_a_removal() => _missingKey.IsRemoval.ShouldBeFalse();
}
