// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverRemover.when_removing;

/// <summary>
/// Ordering is the whole correctness argument here. A live activation holds the observer's definition, failures and
/// reminders in memory and writes them back as it deactivates, so deleting the records first is a race the records
/// win - the removal appears to succeed and the documents are back moments later. The grain is therefore told to stand
/// down before anything is deleted.
/// </summary>
public class and_the_observer_grain_is_still_activated : given.all_dependencies
{
    readonly List<string> _sequence = [];

    void Establish()
    {
        _observerInFirstNamespace.When(observer => observer.Remove()).Do(_ => _sequence.Add("grain stood down"));
        _firstNamespaceStorage.Observers.When(observers => observers.Delete(_observerId)).Do(_ => _sequence.Add("state deleted"));
        _firstNamespaceStorage.FailedPartitions.When(partitions => partitions.RemoveAllFor(_observerId)).Do(_ => _sequence.Add("failed partitions deleted"));
    }

    async Task Because() => await Remove();

    [Fact]
    void should_stand_the_grain_down_before_deleting_its_records() =>
        string.Join(", ", _sequence).ShouldEqual("grain stood down, state deleted, failed partitions deleted");
}
