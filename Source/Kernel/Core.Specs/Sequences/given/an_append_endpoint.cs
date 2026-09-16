// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json.Nodes;
using Cratis.Arc.Authorization;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.EventSequences;
using Microsoft.AspNetCore.Http;

namespace Cratis.Chronicle.Sequences.given;

public class an_append_endpoint : Specification
{
    protected IGrainFactory _grainFactory;
    protected IEventSequence _eventSequence;
    protected RequestCausation _causation;
    protected ICurrentPrincipalAccessor _principal;
    protected EventSequences.EventToAppend[] _appendedEvents;

    void Establish()
    {
        _grainFactory = Substitute.For<IGrainFactory>();
        _eventSequence = Substitute.For<IEventSequence>();
        _grainFactory.GetGrain<IEventSequence>(Arg.Any<string>()).Returns(_eventSequence);
        _causation = new RequestCausation(new HttpContextAccessor());
        _principal = Substitute.For<ICurrentPrincipalAccessor>();
        _principal.Current.Returns(new ClaimsPrincipal());
        _eventSequence.When(_ => _.Append(
            Arg.Any<EventSourceType>(),
            Arg.Any<EventSourceId>(),
            Arg.Any<EventStreamType>(),
            Arg.Any<EventStreamId>(),
            Arg.Any<Concepts.Events.EventType>(),
            Arg.Any<JsonObject>(),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<Concepts.Auditing.Causation>>(),
            Arg.Any<Concepts.Identities.Identity>(),
            Arg.Any<IEnumerable<Tag>>(),
            Arg.Any<Concepts.EventSequences.Concurrency.ConcurrencyScope>(),
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<Subject?>()))
            .Do(call => _appendedEvents = [new EventSequences.EventToAppend(
                call.ArgAt<EventSourceType>(0),
                call.ArgAt<EventSourceId>(1),
                call.ArgAt<EventStreamType>(2),
                call.ArgAt<EventStreamId>(3),
                call.ArgAt<Concepts.Events.EventType>(4),
                call.ArgAt<IEnumerable<Tag>>(9),
                call.ArgAt<JsonObject>(5))]);
        _eventSequence.When(_ => _.AppendMany(
            Arg.Any<IEnumerable<EventSequences.EventToAppend>>(),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<Concepts.Auditing.Causation>>(),
            Arg.Any<Concepts.Identities.Identity>(),
            Arg.Any<ConcurrencyScopes>()))
            .Do(call => _appendedEvents = call.ArgAt<IEnumerable<EventSequences.EventToAppend>>(0).ToArray());
    }
}
