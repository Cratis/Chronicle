// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.for_ReactorDispatcher.when_dispatching_a_page_to_a_batch_reactor;

public class and_the_kernel_does_not_deliver_pages : given.a_batch_reactor_dispatcher
{
    void Establish() => _recorder.OnBatch = batch => batch.Defer(new EventSourceId("first"), TimeSpan.FromSeconds(30));

    Task Because()
    {
        var page = Page(("first", [(1, "registered")]));
        var single = new EventsToObserve { DeliveryId = page.DeliveryId, Partition = "first", Events = page.Partitions[0].Events };
        return Dispatch(single);
    }

    [Fact] void should_answer_with_a_single_partition_result() => _published[0].Partition.ShouldEqual("first");
    [Fact] void should_fail_the_partition_instead_of_deferring_it() => _published[0].State.ShouldEqual(ObservationState.Failed);
    [Fact] void should_say_why() => _published[0].ExceptionMessages.Any(_ => _.Contains("does not accept deferred partitions")).ShouldBeTrue();
}
