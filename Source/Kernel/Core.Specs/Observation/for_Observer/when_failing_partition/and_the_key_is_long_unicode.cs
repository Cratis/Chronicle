// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_failing_partition;

public class and_the_key_is_long_unicode : given.an_observer
{
    string _reminderName;

    async Task Because()
    {
        await _observer.PartitionFailed(new string('界', 1000), 12UL, ["Failure"], "Stack");
        _reminderName = Observer.PartitionReminderName(_failedPartitionsState.Partitions.Single().Id);
    }

    [Fact] void should_bound_the_name_independently_of_the_key() => _reminderName.Length.ShouldEqual(35);
    [Fact] async Task should_register_the_bounded_reminder() => (await _observer.GetReminder(_reminderName)).ShouldNotBeNull();
    [Fact] void should_persist_the_failure() => _failedPartitionsStorageStats.Writes.ShouldEqual(1);
}
