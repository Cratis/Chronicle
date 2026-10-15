// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.EventSequences.for_EventSequence;

public class when_normalizing_explicit_scopes : given.a_scope_correctness_context
{
    readonly List<bool> _narrowedChecks = [];
    readonly List<bool> _strategyChecks = [];
    readonly List<bool> _emptyChecks = [];
    readonly List<bool> _legacyMatches = [];

    async Task Because()
    {
        var strategy = new OptimisticConcurrencyStrategy(_tailSource);
        _tailSource.GetTailSequenceNumber(Arg.Any<EventSourceId?>(), Arg.Any<EventSourceType?>(), Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<IEnumerable<EventType>?>())
            .Returns(EventSequenceNumber.Unavailable);
        var issued = await strategy.GetScope(_sourceId);
        var narrowed = new ConcurrencyScope(EventSequenceNumber.Unavailable, _sourceId);
        ConcurrencyScope[] scopes =
        [
            new ConcurrencyScopeBuilder().WithEventSourceId(_sourceId).Build(),
            narrowed,
            narrowed with { EventStreamId = "first" },
            issued,
            issued with { EventStreamId = "copy" },
            new ConcurrencyScopeBuilder().Build(),
            new(EventSequenceNumber.Unavailable, EventTypes: []),
            ConcurrencyScope.None,
            new(EventSequenceNumber.Unavailable, EventStreamType: EventStreamType.All, EventStreamId: EventStreamId.Default, EventSourceType: EventSourceType.Default)
        ];
        for (var path = 0; path < 11; path++)
        {
            foreach (var legacy in new[] { false, true })
            {
                _options.SkipIncompleteExplicitScopes = legacy;
                for (var index = 0; index < scopes.Length; index++)
                {
                    await AppendOnPath(path, scopes[index]);
                    if (legacy)
                    {
                        var expected = scopes[index].ToSequencesContract();

                        // Enforce normalizes wildcard predicates independently of the legacy switch.
                        _legacyMatches.Add(_wireScope.SequenceNumber == expected.SequenceNumber && _wireScope.ExpectsNoMatchingEvent == expected.ExpectsNoMatchingEvent);
                    }
                    else if (index < 3)
                    {
                        _narrowedChecks.Add(_wireScope.ExpectsNoMatchingEvent);
                    }
                    else if (index < 5)
                    {
                        _strategyChecks.Add(_wireScope.ExpectsNoMatchingEvent);
                    }
                    else
                    {
                        _emptyChecks.Add(_wireScope.ExpectsNoMatchingEvent);
                    }
                }
            }
        }
    }

    [Fact] void should_check_every_narrowed_explicit_scope_on_every_path() => _narrowedChecks.ShouldEachConformTo(_ => _);
    [Fact] void should_preserve_strategy_policy_and_its_with_copy_on_every_path() => _strategyChecks.ShouldEachConformTo(_ => !_);
    [Fact] void should_not_turn_an_empty_builder_or_wildcards_into_a_whole_log_expectation() => _emptyChecks.ShouldEachConformTo(_ => !_);
    [Fact] void should_preserve_legacy_boundaries_on_every_path_when_switched() => _legacyMatches.ShouldEachConformTo(_ => _);
    [Fact] void should_exercise_all_matrix_entries() => _appendCalls.ShouldEqual(198);
}
