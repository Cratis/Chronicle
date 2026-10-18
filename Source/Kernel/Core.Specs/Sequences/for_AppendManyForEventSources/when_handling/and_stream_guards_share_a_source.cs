// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Sequences.for_AppendManyForEventSources.when_handling;

public class and_stream_guards_share_a_source : given.an_append_endpoint
{
    AppendManyForEventSources _command;
    ConcurrencyScopeConjunctions _forwarded;

    void Establish()
    {
        _command = new("store", "namespace", EventSequenceId.Log, [], ConcurrencyScopes:
        [
            new("source", new(5, true, "stream", "first")),
            new("source", new(5, true, "stream", "first")),
            new("source", new(5, true, "stream", "second"))
        ]);
        _eventSequence.AppendManyWithConjunctions(Arg.Any<IEnumerable<EventSequences.EventToAppend>>(), Arg.Any<CorrelationId>(), Arg.Any<IEnumerable<Concepts.Auditing.Causation>>(), Arg.Any<Concepts.Identities.Identity>(), Arg.Any<ConcurrencyScopeConjunctions>())
            .Returns(call =>
            {
                _forwarded = call.Arg<ConcurrencyScopeConjunctions>();
                return Task.FromResult(new AppendManyResult());
            });
    }

    async Task Because() => await _command.Handle(_grainFactory, _causation, _principal);

    [Fact] void should_use_the_new_cluster_method() => _forwarded.Entries.Count.ShouldEqual(2);
    [Fact] void should_keep_the_original_request_position() => _forwarded.Entries[1].ScopeIndex.ShouldEqual((uint?)2);
    [Fact] void should_not_send_repeated_sources_to_the_legacy_codec() => _eventSequence.DidNotReceive().AppendMany(Arg.Any<IEnumerable<EventSequences.EventToAppend>>(), Arg.Any<CorrelationId>(), Arg.Any<IEnumerable<Concepts.Auditing.Causation>>(), Arg.Any<Concepts.Identities.Identity>(), Arg.Any<ConcurrencyScopes>());
}
