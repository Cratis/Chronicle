// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing;

public class and_a_partition_fails : given.a_performing_job_step
{
    void Establish()
    {
        // module#1 succeeds, feature#2 fails, module#3 should never be dispatched. Ordered replay halts at
        // the first failure so it does not layer state onto a partition that failed to materialize.
        _eventCursor.Current.Returns([
            CreateEvent(1UL, "module"),
            CreateEvent(2UL, "feature"),
            CreateEvent(3UL, "module")
        ]);

        _observerSubscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(call =>
            {
                var partition = call.Arg<Key>();
                var events = call.ArgAt<IEnumerable<AppendedEvent>>(1).ToArray();
                _handledBatches.Add(new(partition, events.Select(_ => _.Context.SequenceNumber).ToArray()));
                return partition == (Key)"feature"
                    ? Task.FromResult(ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "boom"))
                    : Task.FromResult(ObserverSubscriberResult.Ok(events[^1].Context.SequenceNumber));
            });
    }

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_handle_the_module_partition_first() => _handledBatches[0].Partition.ShouldEqual((Key)"module");
    [Fact] void should_attempt_the_failing_feature_partition() => _handledBatches[1].Partition.ShouldEqual((Key)"feature");
    [Fact] void should_not_dispatch_anything_after_the_failure() => _handledBatches.Count.ShouldEqual(2);
    [Fact] void should_report_the_failing_partition() => _observer.Received(1).PartitionFailed((Key)"feature", Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<string>>(), Arg.Any<string>());
}
