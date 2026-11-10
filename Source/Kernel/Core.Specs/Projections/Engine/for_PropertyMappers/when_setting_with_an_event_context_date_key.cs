// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_PropertyMappers;

public class when_setting_with_an_event_context_date_key : Specification
{
    PropertyMapper<AppendedEvent, ExpandoObject> _propertyMapper;
    ExpandoObject _target;

    void Establish()
    {
        _target = new();
        _propertyMapper = PropertyMappers.FromEventValueProvider(
            "byOccurred.$eventContext.occurred",
            _ => "value");
    }

    void Because() => _propertyMapper(
        given.an_appended_event.with_context(new DateTimeOffset(2026, 3, 4, 5, 6, 7, 890, TimeSpan.FromHours(2)), Identity.System),
        _target,
        ArrayIndexers.NoIndexers);

    [Fact] void should_use_the_general_invariant_date_format_as_the_key() =>
        ((IDictionary<string, object?>)((dynamic)_target).byOccurred).Keys.ShouldContainOnly("03/04/2026 05:06:07 +02:00");
}
