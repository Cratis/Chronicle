// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_PropertyMappers;

public class when_setting_with_a_missing_event_content_dictionary_key : Specification
{
    PropertyMapper<AppendedEvent, ExpandoObject> _propertyMapper;
    ExpandoObject _target;
    PropertyDifference _missingKey;
    PropertyDifference _nullKey;

    void Establish()
    {
        _target = new();
        _propertyMapper = PropertyMappers.FromEventValueProvider(
            "slots.$eventContent.slot",
            EventValueProviders.EventContent("seriesId"));
    }

    void Because()
    {
        _missingKey = _propertyMapper(given.an_appended_event.with_slot(null, "series-1"), _target, ArrayIndexers.NoIndexers);

        var content = new ExpandoObject();
        ((IDictionary<string, object?>)content)["slot"] = null;
        ((IDictionary<string, object?>)content)["seriesId"] = "series-2";
        _nullKey = _propertyMapper(given.an_appended_event.with_content(content), _target, ArrayIndexers.NoIndexers);
    }

    [Fact] void should_not_change_anything_for_a_missing_key() => _missingKey.HasChanges().ShouldBeFalse();
    [Fact] void should_not_change_anything_for_a_null_key() => _nullKey.HasChanges().ShouldBeFalse();
    [Fact] void should_not_create_the_dictionary() => ((IDictionary<string, object?>)_target).ContainsKey("slots").ShouldBeFalse();
}
