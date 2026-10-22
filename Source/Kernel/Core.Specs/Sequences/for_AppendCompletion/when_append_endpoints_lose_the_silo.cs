// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Sequences.for_AppendCompletion;

public class when_append_endpoints_lose_the_silo : given.an_append_endpoint
{
    readonly Guid _correlationId = Guid.NewGuid();
    AppendResult _single;
    AppendManyResult _singleSource;
    AppendManyResult _multiSource;

    void Establish()
    {
        _eventSequence.Append(
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
            Arg.Any<Subject?>())
            .Returns(Task.FromException<AppendResult>(new SiloUnavailableException()));
        _eventSequence.AppendMany(
            Arg.Any<IEnumerable<EventSequences.EventToAppend>>(),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<Concepts.Auditing.Causation>>(),
            Arg.Any<Concepts.Identities.Identity>(),
            Arg.Any<ConcurrencyScopes>())
            .Returns(Task.FromException<AppendManyResult>(new SiloUnavailableException()));
    }

    async Task Because()
    {
        _single = await new Append("store", "namespace", "event-log", "source", null!, null!, null!, new("event", 1, false), "{}", _correlationId)
            .Handle(_grainFactory, _causation, _principal);
        _singleSource = await new AppendMany("store", "namespace", "event-log", "source", [new(new("event", 1, false), "{}")], _correlationId)
            .Handle(_grainFactory, _causation, _principal);
        _multiSource = await new AppendManyForEventSources("store", "namespace", "event-log", [new("source", "source-type", "stream-type", "stream", new("event", 1, false), "{}")], _correlationId)
            .Handle(_grainFactory, _causation, _principal);
    }

    [Fact] void should_settle_the_single_append() => _single.Errors.ShouldContainOnly([AppendError.AppendOutcomeUnknown]);
    [Fact] void should_settle_the_single_source_batch() => _singleSource.Errors.ShouldContainOnly([AppendError.AppendOutcomeUnknown]);
    [Fact] void should_settle_the_multi_source_batch() => _multiSource.Errors.ShouldContainOnly([AppendError.AppendOutcomeUnknown]);
    [Fact] void should_preserve_the_correlation_id() => _multiSource.CorrelationId.Value.ShouldEqual(_correlationId);
}
