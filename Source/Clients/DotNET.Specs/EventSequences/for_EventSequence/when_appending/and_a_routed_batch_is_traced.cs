// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Identities;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class and_a_routed_batch_is_traced : given.all_dependencies
{
    readonly List<Activity> _activities = [];
    ActivitySource _activitySource;
    ActivityListener _listener;
    Activity _scopeActivity;
    EventSequence _eventSequence;

    void Establish()
    {
        _activitySource = new ActivitySource("routed-batch-specification");
        _listener = new ActivityListener
        {
            ShouldListenTo = source => ReferenceEquals(source, _activitySource),
            Sample = (ref _) => ActivitySamplingResult.AllData,
            ActivityStarted = _activities.Add
        };
        ActivitySource.AddActivityListener(_listener);
        _eventTypes.HasFor(typeof(string)).Returns(true);
        _eventTypes.GetEventTypeFor(typeof(string)).Returns(new EventType("event", EventTypeGeneration.First));
        _eventSerializer.Serialize(Arg.Any<object>()).Returns(new JsonObject());
        _identityProvider.GetCurrent().Returns(new Identity("caller", "Caller", "caller", null));
        _concurrencyScopeStrategy.GetScope("source", "Payments", EventStreamId.Default, "Account", default).Returns(_ =>
        {
            _scopeActivity = Activity.Current;
            return _defaultConcurrencyScope;
        });
        _sequences.AppendManyForEventSources(Arg.Any<Contracts.Sequences.AppendManyForEventSourcesRequest>(), Arg.Any<CallContext>())
            .Returns(call => CommandResult<Contracts.Sequences.AppendManyResponse>.Success(Guid.Empty, given.append_receipts.Complete(new() { SequenceNumbers = [42] }, call.Arg<Contracts.Sequences.AppendManyForEventSourcesRequest>())));
        _eventSequence = new(
            "store",
            "namespace",
            "event-log",
            _connection,
            _eventTypes,
            _constraints,
            _eventSerializer,
            _correlationIdAccessor,
            _concurrencyScopeStrategies,
            _causationManager,
            _unitOfWorkManager,
            _identityProvider,
            JsonSerializerOptions.Default,
            new Cratis.Traces.ActivitySource<EventSequence>(_activitySource));
    }

    async Task Because() => await _eventSequence.AppendMany("source", ["event"], eventStreamType: "Payments", eventSourceType: "Account");

    [Fact] void should_emit_one_append_span() => _activities.Count.ShouldEqual(1);
    [Fact] void should_name_the_append_operation() => _activities[0].OperationName.ShouldEqual("client.event_sequence.append_many");
    [Fact] void should_include_scope_resolution_in_the_span() => _scopeActivity.ShouldEqual(_activities.Single());
    [Fact] async Task should_resolve_the_scope_only_once() => await _concurrencyScopeStrategy.Received(1).GetScope("source", "Payments", EventStreamId.Default, "Account", default);

    void Destroy()
    {
        _listener.Dispose();
        _activitySource.Dispose();
    }
}
