// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ReducerObserverSubscriber.when_receiving_events;

/// <summary>Notification errors follow the projection subscriber's failure behavior.</summary>
public class and_notification_fails : given.a_subscriber_with_a_change
{
    void Establish() => _notifier.Notify(_key.Namespace, _change.ReadModelKey, _change.ReadModel, _change.Change).ThrowsAsync(new InvalidOperationException("notification failed"));

    Task Because() => ReceiveEvents();

    [Fact] void should_report_failure() => _result.State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_not_advance_the_observer() => _result.LastSuccessfulObservation.ShouldEqual(Concepts.Events.EventSequenceNumber.Unavailable);
}
