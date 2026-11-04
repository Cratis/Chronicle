// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.InMemory;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexPoisonTracker;

[PerFactSpecificationLifecycle]
public class when_observing_failures_with_a_configured_threshold : Specification
{
    readonly ConstraintIndexPoisonTracker _tracker = new(new InMemoryStorageErrorClassifier());

    [Fact] void should_not_settle_before_the_configured_threshold() => Observe(2).ShouldBeEmpty();
    [Fact] void should_settle_at_the_configured_threshold() => Observe(3).Keys.ShouldContain((ConstraintName)"unique");

    IReadOnlyDictionary<ConstraintName, ConstraintIndexReplayFailure> Observe(int attempts)
    {
        IReadOnlyDictionary<ConstraintName, ConstraintIndexReplayFailure> result = new Dictionary<ConstraintName, ConstraintIndexReplayFailure>();
        for (var attempt = 0; attempt < attempts; attempt++)
        {
            result = _tracker.Observe(new Dictionary<ConstraintName, ConstraintIndexReplayFailure> { ["unique"] = new(EventSequenceNumber.First, new FormatException()) }, true, 3);
        }
        return result;
    }
}
