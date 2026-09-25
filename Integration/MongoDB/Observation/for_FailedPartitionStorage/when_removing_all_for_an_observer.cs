// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using context = Cratis.Chronicle.MongoDB.Integration.Observation.for_FailedPartitionStorage.when_removing_all_for_an_observer.context;

namespace Cratis.Chronicle.MongoDB.Integration.Observation.for_FailedPartitionStorage;

/// <summary>
/// Saving an empty set of failed partitions only clears the ones it is told are resolved, so it leaves a partition
/// that failed and was never resolved exactly where it was. Removing an observer needs the stronger guarantee that
/// nothing keyed to it survives it - and needs it to stop at that observer.
/// </summary>
/// <param name="context">The <see cref="context"/> the specification runs against.</param>
[Collection(MongoDBCollection.Name)]
public class when_removing_all_for_an_observer(context context) : MongoDBGiven<context>(context)
{
    public class context(ChronicleInProcessFixture fixture) : given.a_failed_partition_storage(fixture)
    {
        public static readonly ObserverId TheObserver = "the-observer";
        public static readonly ObserverId AnotherObserver = "another-observer";

        public FailedPartitions RemainingForTheObserver = default!;
        public FailedPartitions RemainingForTheOtherObserver = default!;

        async Task Because()
        {
            await _storage.Save(TheObserver, new FailedPartitions
            {
                Partitions =
                [
                    CreateFailedPartition(TheObserver, "first-partition"),
                    CreateFailedPartition(TheObserver, "second-partition")
                ]
            });

            await _storage.Save(AnotherObserver, new FailedPartitions
            {
                Partitions = [CreateFailedPartition(AnotherObserver, "first-partition")]
            });

            await _storage.RemoveAllFor(TheObserver);

            RemainingForTheObserver = await _storage.GetFor(TheObserver);
            RemainingForTheOtherObserver = await _storage.GetFor(AnotherObserver);
        }
    }

    [Fact] void should_remove_every_failed_partition_for_the_observer() => Context.RemainingForTheObserver.Partitions.ShouldBeEmpty();
    [Fact] void should_leave_the_other_observers_failed_partitions_alone() => Context.RemainingForTheOtherObserver.Partitions.Count().ShouldEqual(1);
}
