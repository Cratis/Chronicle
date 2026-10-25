// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Identities;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class an_event_sequence_with_identifier_tracing : all_dependencies
{
    protected const string Key = "chronicle-identifier-hash-key-for-specs-0001";
    protected const string SourceId = "customer-42";
    protected Activity _append;
    protected KeyValuePair<string, object?>[] _startedTags;
    protected Contracts.Sequences.AppendRequest _request;
    ActivitySource _source;
    ActivityListener _listener;
    EventSequence _sequence;

    protected virtual ChronicleTelemetryOptions CreateTelemetry() => new();

    void Establish()
    {
        _source = new ActivitySource($"identifier-tracing-{Guid.NewGuid():N}");
        _listener = new()
        {
            ShouldListenTo = source => ReferenceEquals(source, _source),
            Sample = (ref _) => ActivitySamplingResult.AllData,
            ActivityStarted = activity => _startedTags = activity.TagObjects.ToArray(),
            ActivityStopped = activity => _append = activity
        };
        ActivitySource.AddActivityListener(_listener);
        _eventTypes.HasFor(typeof(string)).Returns(true);
        _eventTypes.GetEventTypeFor(typeof(string)).Returns(new EventType("event", EventTypeGeneration.First));
        _eventSerializer.Serialize(Arg.Any<object>()).Returns(new JsonObject());
        _identityProvider.GetCurrent().Returns(Identity.System);
        _sequences.Append(Arg.Any<Contracts.Sequences.AppendRequest>(), Arg.Any<CallContext>()).Returns(call =>
        {
            _request = call.Arg<Contracts.Sequences.AppendRequest>();
            return CommandResult<Contracts.Sequences.AppendResponse>.Success(Guid.Empty, new() { SequenceNumber = 42 });
        });
        _sequence = new(
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
            new Cratis.Traces.ActivitySource<EventSequence>(_source),
            telemetry: CreateTelemetry());
    }

    protected Task Append() => _sequence.Append(SourceId, "event", concurrencyScope: ConcurrencyScope.None);

    void Destroy()
    {
        _listener.Dispose();
        _source.Dispose();
    }
}
