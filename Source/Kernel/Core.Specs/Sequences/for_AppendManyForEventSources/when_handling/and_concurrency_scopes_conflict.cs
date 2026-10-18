// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Sequences.for_AppendManyForEventSources.when_handling;

public class and_concurrency_scopes_conflict : given.an_append_endpoint
{
    readonly Guid _correlationId = Guid.NewGuid();
    AppendManyForEventSources _command;
    AppendManyResult _result;

    void Establish() => _command = new(
        "store",
        "namespace",
        EventSequenceId.Log,
        [new("source", "source-type", "stream-type", "stream", new("event", 1, false), "{}")],
        CorrelationId: _correlationId,
        ConcurrencyScopes:
        [
            new("source", new(ulong.MaxValue, false)),
            new("source", new(5, true))
        ]);

    async Task Because() => _result = await _command.Handle(_grainFactory, _causation, _principal);

    [Fact] void should_reject_the_append() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_the_conflicting_source() => _result.Errors.ShouldContainOnly(new AppendError("Conflicting concurrency scopes for event source 'source'."));
    [Fact] void should_preserve_the_correlation_id() => _result.CorrelationId.ShouldEqual((CorrelationId)_correlationId);
    [Fact] void should_not_forward_the_batch_to_the_grain() => _eventSequence.DidNotReceive().AppendMany(Arg.Any<IEnumerable<EventSequences.EventToAppend>>(), Arg.Any<CorrelationId>(), Arg.Any<IEnumerable<Concepts.Auditing.Causation>>(), Arg.Any<Concepts.Identities.Identity>(), Arg.Any<ConcurrencyScopes>());
    [Fact] void should_not_report_a_concurrency_check() => _result.ConcurrencyCheckPerformed.ShouldBeFalse();
}
