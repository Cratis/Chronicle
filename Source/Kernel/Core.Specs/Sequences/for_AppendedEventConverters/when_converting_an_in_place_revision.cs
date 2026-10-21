// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Sequences.for_AppendedEventConverters;

public class when_converting_an_in_place_revision : Specification
{
    Concepts.Events.AppendedEvent _event;
    AppendedEvent _result;

    void Establish() =>
        _event = new Concepts.Events.AppendedEvent(
            Concepts.Events.EventContext.From("store", "default", new Concepts.Events.EventType("stock-set", 2), EventSourceType.Default, "source", EventStreamType.All, EventStreamId.Default, 0, CorrelationId.NotSet),
            new ExpandoObject())
        {
            RevisedGeneration = 2
        };

    void Because() => _result = _event.ToApi(new JsonSerializerOptions());

    [Fact] void should_carry_the_revised_generation_without_revision_history() => _result.RevisedGeneration.ShouldEqual(2u);
    [Fact] void should_have_no_revisions() => _result.Revisions.ShouldBeEmpty();
}
