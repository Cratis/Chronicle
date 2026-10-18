// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Sequences.for_AppendManyForEventSources.when_handling;

public class and_a_duplicate_precedes_a_legacy_scope_violation : given.an_append_endpoint
{
    AppendManyForEventSources _command;
    AppendManyResult _result;

    void Establish()
    {
        _command = new("store", "namespace", EventSequenceId.Log, [], ConcurrencyScopes:
        [
            new("first", new(5, true)),
            new("first", new(5, true)),
            new("second", new(5, true))
        ]);
        _eventSequence.AppendMany(Arg.Any<IEnumerable<EventSequences.EventToAppend>>(), Arg.Any<CorrelationId>(), Arg.Any<IEnumerable<Concepts.Auditing.Causation>>(), Arg.Any<Concepts.Identities.Identity>(), Arg.Any<ConcurrencyScopes>())
            .Returns(Task.FromResult(new AppendManyResult { ConcurrencyViolations = [new ConcurrencyViolation("second", 5UL, 6UL)] }));
    }

    async Task Because() => _result = await _command.Handle(_grainFactory, _causation, _principal);

    [Fact] void should_report_the_original_position_even_on_the_legacy_grain_path() => _result.ConcurrencyViolations.Single().ScopeIndex.ShouldEqual((uint?)2);
}
