// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.for_PropertyMappers;

public class when_counting_with_dynamic_dictionary_key_from_caused_by : Specification
{
    ExpandoObject _target;

    void Because()
    {
        var mapper = PropertyMappers.Count(new TypeFormats(), "counts.$causedBy.userName", new JsonSchemaProperty { Type = JsonObjectType.Integer, Format = "int64" });
        var @event = new AppendedEvent(
            new(
                new("02405794-91e7-4e4f-8ad1-f043070ca297", 1),
                EventSourceType.Default,
                "2f005aaf-2f4e-4a47-92ea-63687ef74bd4",
                EventStreamType.All,
                EventStreamId.Default,
                0,
                DateTimeOffset.UtcNow,
                "123b8935-a1a4-410d-aace-e340d48f0aa0",
                "41f18595-4748-4b01-88f7-4c0d0907aa90",
                CorrelationId.New(),
                [],
                new Identity("subject-1", "Test User", "test-user"),
                [],
                EventHash.NotSet),
            new ExpandoObject());
        _target = new();
        mapper(@event, _target, ArrayIndexers.NoIndexers);
    }

    [Fact] void should_count_under_the_resolved_user_name() => ((IDictionary<string, object>)((dynamic)_target).counts)["test-user"].ShouldEqual(1L);
    [Fact] void should_not_use_the_expression_as_a_literal_key() => ((IDictionary<string, object>)((dynamic)_target).counts).ContainsKey("$causedBy").ShouldBeFalse();
}
