// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_PropertyMappers;

public class when_setting_with_a_null_event_context_key : Specification
{
    PropertyMapper<AppendedEvent, ExpandoObject> _propertyMapper;
    ExpandoObject _target;
    PropertyDifference _result;

    void Establish()
    {
        _target = new();
        _propertyMapper = PropertyMappers.FromEventValueProvider(
            "byDelegate.$eventContext.causedBy.onBehalfOf.name",
            _ => "value");
    }

    void Because() => _result = _propertyMapper(
        given.an_appended_event.with_context(DateTimeOffset.UtcNow, new Identity("subject-1", "John Doe", "john")),
        _target,
        ArrayIndexers.NoIndexers);

    [Fact] void should_not_change_anything() => _result.HasChanges().ShouldBeFalse();
    [Fact] void should_not_create_the_dictionary() => ((IDictionary<string, object?>)_target).ContainsKey("byDelegate").ShouldBeFalse();
}
