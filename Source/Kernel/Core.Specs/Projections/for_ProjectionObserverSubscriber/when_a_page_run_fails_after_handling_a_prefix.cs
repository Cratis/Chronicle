// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections.Engine;
using NSubstitute.Extensions;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber;

public class when_a_page_run_fails_after_handling_a_prefix : given.a_page_subscriber
{
    ObserverSubscriberResult _result;

    void Establish()
    {
        _events = [_events[0], _events[1], _events[1] with { Context = _events[1].Context with { SequenceNumber = 3 } }, _events[2] with { Context = _events[2].Context with { SequenceNumber = 4 } }];
        _cachedPipeline.Configure().Handle(_events[2]).Returns(Task.FromException<ProjectionEventContext>(new InvalidOperationException("boom")));
    }

    async Task Because() => _result = await _subscriber.OnNextPage(_events, new(null));

    [Fact] void should_stop_before_later_events() => _handled.ShouldEqual(new EventSequenceNumber[] { 1, 2 });
    [Fact] void should_not_notify_the_failing_runs_handled_prefix() => _notifier.Received(1).NotifyMany(_key.Namespace, Arg.Is<IReadOnlyList<ReadModelChangeNotification>>(_ => _.Count == 1 && _[0].ReadModelKey == (ReadModelKey)"parent" && _[0].Change.EventSequenceNumber == (EventSequenceNumber)1UL));
    [Fact] void should_return_failed() => _result.State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_keep_the_handled_prefix_position() => _result.LastSuccessfulObservation.ShouldEqual((EventSequenceNumber)2UL);
    [Fact] void should_name_the_failing_partition() => _result.FailedPartition.ShouldEqual((Key)"child");
    [Fact] void should_name_the_failing_sequence() => _result.FailedAt.ShouldEqual((EventSequenceNumber)3UL);
}
