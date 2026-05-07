// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Contracts.EventSequences;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Identities;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class many_known_events_and_causation_manager_returns_null : given.an_event_sequence
{
    EventSourceId _eventSourceId;
    List<string> _events;
    EventType _eventType;
    AppendManyRequest _request;
    List<JsonObject> _eventContexts;

    void Establish()
    {
        _eventSourceId = Guid.NewGuid();
        _events = ["Event1", "Event2", "Event3"];
        _eventType = new(Guid.NewGuid().ToString(), EventTypeGeneration.First);
        _eventContexts = _events.ConvertAll(e => new JsonObject { ["value"] = e });

        for (var i = 0; i < _events.Count; i++)
        {
            _eventSerializer.Serialize(_events[i]).Returns(_eventContexts[i]);
        }

        _eventTypes.HasFor(typeof(string)).Returns(true);
        _eventTypes.GetEventTypeFor(typeof(string)).Returns(_eventType);
        _eventSequences
            .When(_ => _.AppendMany(Arg.Any<AppendManyRequest>(), CallContext.Default))
            .Do(callInfo => _request = callInfo.Arg<AppendManyRequest>());

        _causationManager.GetCurrentChain().Returns((System.Collections.Immutable.IImmutableList<Causation>)null!);
        _identityProvider.GetCurrent().Returns(new Identity("Subject", "Name", "UserName"));

        _serviceAccessor.Services.EventSequences.AppendMany(Arg.Any<AppendManyRequest>(), CallContext.Default).Returns(new AppendManyResponse
        {
            CorrelationId = Guid.NewGuid(),
            SequenceNumbers = [42, 43, 44],
            ConstraintViolations = [],
            Errors = []
        });
    }

    async Task Because() => await _eventSequence.AppendMany(_eventSourceId, _events, concurrencyScope: ConcurrencyScope.None);

    [Fact] void should_append_events() => _request.ShouldNotBeNull();
    [Fact] void should_send_empty_causation_chain() => _request.Causation.ShouldBeEmpty();
    [Fact] void should_send_explicit_concurrency_scope() => _request.ConcurrencyScopes.ContainsKey(_eventSourceId.Value).ShouldBeTrue();
}
