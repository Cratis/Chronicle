// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Microsoft.Extensions.Logging;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ReducerObserverSubscriber.when_receiving_events;

/// <summary>Notification failures must not retry an already committed reducer write.</summary>
public class and_notification_fails : given.a_subscriber_with_a_change
{
    readonly Exception _error = new InvalidOperationException("notification failed");

    void Establish() => _notifier.Notify(_key.Namespace, _change.ReadModelKey, _change.ReadModel, _change.Change).ThrowsAsync(_error);

    Task Because() => ReceiveEvents();

    [Fact] void should_commit_before_notifying() => _sinkReached.ShouldBeTrue();
    [Fact] void should_notify_only_after_commit() => _notifiedBeforeCommit.ShouldBeFalse();
    [Fact] void should_not_fail_the_partition() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
    [Fact] void should_advance_past_the_committed_events() => _result.LastSuccessfulObservation.ShouldEqual((EventSequenceNumber)42);
    [Fact] void should_not_retry_the_committed_write() => _firstPipeline.Received(1).Reduce(Arg.Any<ReducerContext>(), Arg.Any<ReducerDelegate>());
    [Fact] void should_not_retry_the_notification() => _notifier.Received(1).Notify(_key.Namespace, _change.ReadModelKey, _change.ReadModel, _change.Change);
    [Fact] void should_log_the_notification_failure() => _logger.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(ILogger.Log) && call.GetArguments()[0] is LogLevel.Error && ReferenceEquals(call.GetArguments()[3], _error)).ShouldEqual(1);
}
