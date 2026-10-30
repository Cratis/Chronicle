// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections.Engine;
using NSubstitute.Extensions;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber;

public class when_a_page_run_fails_and_notifying_fails : given.a_page_subscriber
{
    ObserverSubscriberResult _result;

    void Establish()
    {
        _cachedPipeline.Configure().Handle(_events[1]).Returns(Task.FromException<ProjectionEventContext>(new InvalidOperationException("run failed")));
        _notifier.NotifyMany(_key.Namespace, Arg.Any<IReadOnlyList<ReadModelChangeNotification>>())
            .Returns(Task.FromException(new InvalidOperationException("notify failed")));
    }

    async Task Because() => _result = await _subscriber.OnNextPage(_events, new(null));

    [Fact] void should_return_failed() => _result.State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_keep_the_runs_failure() => _result.ExceptionMessages.ShouldContain("run failed");
    [Fact] void should_name_the_failing_runs_partition() => _result.FailedPartition.ShouldEqual((Key)"child");
    [Fact] void should_name_the_failing_runs_sequence() => _result.FailedAt.ShouldEqual((EventSequenceNumber)2UL);
    [Fact] void should_keep_the_position_before_the_failing_run() => _result.LastSuccessfulObservation.ShouldEqual((EventSequenceNumber)1UL);
}
