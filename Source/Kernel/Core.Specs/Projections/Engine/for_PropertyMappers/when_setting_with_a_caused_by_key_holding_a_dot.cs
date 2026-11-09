// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_PropertyMappers;

public class when_setting_with_a_caused_by_key_holding_a_dot : Specification
{
    PropertyMapper<AppendedEvent, ExpandoObject> _propertyMapper;
    ExpandoObject _target;
    Exception _error;

    void Establish()
    {
        _target = new();
        _propertyMapper = PropertyMappers.FromEventValueProvider(
            "lastSeenBy.$causedBy.userName",
            _ => "seen");
    }

    void Because() => _error = Catch.Exception(() => _propertyMapper(
        given.an_appended_event.with_context(DateTimeOffset.UtcNow, new Identity("subject-1", "John Doe", "john.doe")),
        _target,
        ArrayIndexers.NoIndexers));

    [Fact] void should_not_fail_the_event() => _error.ShouldBeNull();
    [Fact] void should_keep_writing_where_earlier_versions_wrote() => ((string)((dynamic)_target).lastSeenBy.john.doe).ShouldEqual("seen");
}
