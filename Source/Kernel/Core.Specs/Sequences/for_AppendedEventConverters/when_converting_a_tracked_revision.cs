// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Sequences.for_AppendedEventConverters;

public class when_converting_a_tracked_revision : Specification
{
    Concepts.Events.AppendedEvent _event;
    AppendedEvent _result;

    void Establish() =>
        _event = new Concepts.Events.AppendedEvent(
            Concepts.Events.EventContext.From("store", "default", new Concepts.Events.EventType("stock-set", 1), EventSourceType.Default, "source", EventStreamType.All, EventStreamId.Default, 0, CorrelationId.NotSet),
            new ExpandoObject())
        {
            Revisions = [new Concepts.Events.EventRevision(1, CorrelationId.NotSet, [], Concepts.Identities.Identity.System, DateTimeOffset.UtcNow), new Concepts.Events.EventRevision(3, CorrelationId.NotSet, [], Concepts.Identities.Identity.System, DateTimeOffset.UtcNow)]
        };

    void Because() => _result = _event.ToApi(new JsonSerializerOptions());

    [Fact] void should_carry_the_generation_of_the_latest_revision() => _result.RevisedGeneration.ShouldEqual(3u);
}
