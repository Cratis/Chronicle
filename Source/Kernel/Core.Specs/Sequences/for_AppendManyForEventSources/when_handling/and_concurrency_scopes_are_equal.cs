// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Sequences.for_AppendManyForEventSources.when_handling;

public class and_concurrency_scopes_are_equal : given.an_append_endpoint
{
    AppendManyForEventSources _command;
    AppendManyResult _result;
    readonly AppendManyResult _grainResult = new();
    ConcurrencyScopes _forwardedScopes;

    void Establish()
    {
        _command = new(
            "store",
            "namespace",
            EventSequenceId.Log,
            [new("source", "source-type", "stream-type", "stream", new("event", 1, false), "{}")],
            ConcurrencyScopes:
            [
                new("source", new(0, true, ExpectsNoMatchingEvent: true)),
                new("source", new(0, true, ExpectsNoMatchingEvent: true))
            ]);
        _eventSequence.AppendMany(Arg.Any<IEnumerable<EventSequences.EventToAppend>>(), Arg.Any<CorrelationId>(), Arg.Any<IEnumerable<Concepts.Auditing.Causation>>(), Arg.Any<Concepts.Identities.Identity>(), Arg.Any<ConcurrencyScopes>())
            .Returns(call =>
            {
                _forwardedScopes = call.Arg<ConcurrencyScopes>();
                return Task.FromResult(_grainResult);
            });
    }

    async Task Because() => _result = await _command.Handle(_grainFactory, _causation, _principal);

    [Fact] void should_forward_only_one_scope() => _forwardedScopes.Scopes.Count.ShouldEqual(1);
    [Fact] void should_preserve_the_freshness_expectation() => _forwardedScopes.GetFor("source").SequenceNumber.ShouldEqual(EventSequenceNumber.BeforeFirst);
    [Fact] void should_return_the_grain_result() => _result.ShouldBeSame(_grainResult);
    [Fact] void should_forward_every_event() => _appendedEvents.Length.ShouldEqual(1);
}
