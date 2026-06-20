// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing;

public class and_child_partition_depends_on_parent_partition : given.a_performing_job_step
{
    bool _parentWasMaterialized;
    bool _childWasResolved;

    void Establish()
    {
        _eventCursor.Current.Returns([
            CreateEvent(1UL, "parent"),
            CreateEvent(2UL, "child")
        ]);

        _observerSubscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(call =>
            {
                var partition = call.Arg<Key>();
                var events = call.ArgAt<IEnumerable<AppendedEvent>>(1).ToArray();
                _handledBatches.Add(new(partition, events.Select(_ => _.Context.SequenceNumber).ToArray()));
                if (partition == (Key)"parent")
                {
                    _parentWasMaterialized = true;
                    return Task.FromResult(ObserverSubscriberResult.Ok(events[^1].Context.SequenceNumber));
                }

                if (!_parentWasMaterialized)
                {
                    return Task.FromResult(ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "Parent was not materialized"));
                }

                _childWasResolved = true;
                return Task.FromResult(ObserverSubscriberResult.Ok(events[^1].Context.SequenceNumber));
            });
    }

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_materialize_parent() => _parentWasMaterialized.ShouldBeTrue();
    [Fact] void should_resolve_child_after_parent() => _childWasResolved.ShouldBeTrue();
    [Fact] void should_not_report_partition_failed() => _observer.DidNotReceive().PartitionFailed(Arg.Any<Key>(), Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<string>>(), Arg.Any<string>());
    [Fact] void should_handle_parent_before_child() => _handledBatches[0].Partition.ShouldEqual((Key)"parent");
    [Fact] void should_handle_child_after_parent() => _handledBatches[1].Partition.ShouldEqual((Key)"child");
}
