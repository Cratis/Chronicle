// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.for_FailedPartitions;

public class when_getting_the_changes_since_persisted : Specification
{
    FailedPartitions _failedPartitions;
    FailedPartitions _firstChanges;
    FailedPartitions _unchanged;
    FailedPartitions _afterAnAttempt;
    FailedPartitions _afterQuarantine;
    FailedPartitions _afterRemoval;
    FailedPartitions _afterRemovalWasPersisted;

    void Establish()
    {
        _failedPartitions = new();
        _failedPartitions.RegisterAttempt("a", 1L, ["failed"], string.Empty);
        _failedPartitions.RegisterAttempt("b", 1L, ["failed"], string.Empty);
        _failedPartitions.RegisterAttempt("c", 1L, ["failed"], string.Empty);
    }

    void Because()
    {
        _firstChanges = _failedPartitions.GetChangesSincePersisted();
        _failedPartitions.MarkPersisted(_firstChanges);
        _unchanged = _failedPartitions.GetChangesSincePersisted();

        _failedPartitions.RegisterAttempt("a", 2L, ["failed again"], string.Empty);
        _afterAnAttempt = _failedPartitions.GetChangesSincePersisted();
        _failedPartitions.MarkPersisted(_afterAnAttempt);

        _failedPartitions.Quarantine("b");
        _afterQuarantine = _failedPartitions.GetChangesSincePersisted();
        _failedPartitions.MarkPersisted(_afterQuarantine);

        _failedPartitions.Remove("c");
        _afterRemoval = _failedPartitions.GetChangesSincePersisted();
        _failedPartitions.MarkPersisted(_afterRemoval);
        _afterRemovalWasPersisted = _failedPartitions.GetChangesSincePersisted();
    }

    [Fact] void should_have_everything_as_changes_before_anything_is_persisted() => _firstChanges.Partitions.Count().ShouldEqual(3);
    [Fact] void should_have_no_changes_when_nothing_changed() => (_unchanged.Partitions.Count() + _unchanged.ResolvedPartitions.Count()).ShouldEqual(0);
    [Fact] void should_have_only_the_partition_with_a_new_attempt() => _afterAnAttempt.Partitions.Select(_ => _.Partition).ShouldContainOnly("a");
    [Fact] void should_have_only_the_partition_that_was_quarantined() => _afterQuarantine.Partitions.Select(_ => _.Partition).ShouldContainOnly("b");
    [Fact] void should_have_the_removed_partition_as_resolved() => _afterRemoval.ResolvedPartitions.Select(_ => _.Partition).ShouldContainOnly("c");
    [Fact] void should_not_have_the_removed_partition_again_once_persisted() => (_afterRemovalWasPersisted.Partitions.Count() + _afterRemovalWasPersisted.ResolvedPartitions.Count()).ShouldEqual(0);
}
