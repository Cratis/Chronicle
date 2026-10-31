// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;
using Microsoft.AspNetCore.Http;

// Conformance: Screenplay relies on this (Cratis/Chronicle#4658).
namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_with_routing.given;

public class a_routed_event_sequence : for_EventSequence.given.appending_many_events
{
    protected IGrainFactory _grainFactory;
    protected Sequences.RequestCausation _causation;
    protected ICurrentPrincipalAccessor _principal;
    protected (EventSourceType SourceType, EventStreamType StreamType, EventStreamId StreamId)[] _storedRoutes;

    void Establish()
    {
        _grainFactory = Substitute.For<IGrainFactory>();
        _grainFactory.GetGrain<IEventSequence>(Arg.Any<string>()).Returns(_eventSequence);
        _causation = new Sequences.RequestCausation(new HttpContextAccessor());
        _principal = Substitute.For<ICurrentPrincipalAccessor>();
        _principal.Current.Returns(new ClaimsPrincipal());
        _storedRoutes = [];

        _eventSequenceStorage.When(storage => storage.Append(
            Arg.Any<EventSequenceNumber>(),
            Arg.Any<EventSourceType>(),
            Arg.Any<EventSourceId>(),
            Arg.Any<EventStreamType>(),
            Arg.Any<EventStreamId>(),
            Arg.Any<EventType>(),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<Causation>>(),
            Arg.Any<IEnumerable<IdentityId>>(),
            Arg.Any<IEnumerable<Tag>>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<IDictionary<EventTypeGeneration, ExpandoObject>>(),
            Arg.Any<IDictionary<EventTypeGeneration, EventHash>>(),
            Arg.Any<Subject?>()))
            .Do(call => _storedRoutes = [(call.ArgAt<EventSourceType>(1), call.ArgAt<EventStreamType>(3), call.ArgAt<EventStreamId>(4))]);

        _eventSequenceStorage.AppendMany(Arg.Any<IEnumerable<EventToAppendToStorage>>())
            .Returns(call =>
            {
                var events = call.Arg<IEnumerable<EventToAppendToStorage>>().ToArray();
                _storedRoutes = events.Select(@event => (@event.EventSourceType, @event.EventStreamType, @event.EventStreamId)).ToArray();
                return Task.FromResult(Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber>.Success(AppendedEventsFrom(events)));
            });
    }
}
