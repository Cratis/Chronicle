// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ReducerObserverSubscriber.when_receiving_events;

/// <summary>Committed reducer changes are sent to the notifier.</summary>
public class and_the_pipeline_reports_a_change : given.a_subscriber_with_a_change
{
    Task Because() => ReceiveEvents();

    [Fact] void should_notify_the_change() => _notifier.Received(1).Notify(_key.Namespace, _change.ReadModelKey, _change.ReadModel, _change.Change);
    [Fact] void should_notify_only_after_commit() => _notifiedBeforeCommit.ShouldBeFalse();
    [Fact] void should_preserve_success() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
}
