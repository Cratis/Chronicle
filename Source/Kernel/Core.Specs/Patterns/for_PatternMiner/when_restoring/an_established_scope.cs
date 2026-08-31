// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns.for_PatternMiner.when_restoring;

/// <summary>
/// The sketch dies with its process while the patterns it survived into do not. Restoring what was persisted
/// must give back exactly the behavior that was established - same combinations, same counts, same support and
/// confidence - so mining continues where it left off instead of starting over.
/// </summary>
public class an_established_scope : given.a_pattern_miner
{
    EventStoreName _restoredInto;
    BehaviorPattern[] _established;
    BehaviorPattern[] _restored;

    void Establish()
    {
        _restoredInto = "store-after-restart";

        for (var count = 0; count < 15; count++)
        {
            _miner.Observe(_eventStore, _namespace, Features("user-42", "ApproveExpenseReport"));
        }

        for (var count = 0; count < 5; count++)
        {
            _miner.Observe(_eventStore, _namespace, Features("user-42", "RejectExpenseReport"));
        }

        _established = [.. _miner.GetSurvivingPatterns(_eventStore, _namespace, "user-42")];
    }

    void Because()
    {
        _miner.Restore(_restoredInto, _namespace, "user-42", _established);
        _restored = [.. _miner.GetSurvivingPatterns(_restoredInto, _namespace, "user-42")];
    }

    BehaviorPattern MatchOf(BehaviorPattern pattern) => _restored.Single(_ => _.Facets.Key == pattern.Facets.Key);

    [Fact] void should_hold_every_established_pattern() =>
        _restored.Select(_ => _.Facets.Key).Order().ShouldContainOnly(_established.Select(_ => _.Facets.Key).Order());

    [Fact] void should_hold_the_same_counts() =>
        _established.All(_ => MatchOf(_).Occurrences == _.Occurrences).ShouldBeTrue();

    [Fact] void should_hold_the_same_support() =>
        _established.All(_ => MatchOf(_).Support == _.Support).ShouldBeTrue();

    [Fact] void should_hold_the_same_confidence() =>
        _established.All(_ => MatchOf(_).Confidence == _.Confidence).ShouldBeTrue();

    [Fact] void should_preserve_when_the_behavior_was_seen() =>
        _established.All(_ => MatchOf(_).FirstSeen == _.FirstSeen && MatchOf(_).LastSeen == _.LastSeen).ShouldBeTrue();
}
