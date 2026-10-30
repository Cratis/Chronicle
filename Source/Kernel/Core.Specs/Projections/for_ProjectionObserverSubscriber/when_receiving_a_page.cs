// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber;

public class when_receiving_a_page : given.a_page_subscriber
{
    static readonly ReadModelChangeType[] _expectedChangeTypes = [ReadModelChangeType.Added, ReadModelChangeType.Modified, ReadModelChangeType.Removed];

    ObserverSubscriberResult _result;
    IReadOnlyList<ReadModelChangeNotification> _notifications;

    void Establish() => _notifier.NotifyMany(_key.Namespace, Arg.Any<IReadOnlyList<ReadModelChangeNotification>>()).Returns(call =>
    {
        _notifications = call.Arg<IReadOnlyList<ReadModelChangeNotification>>();
        return Task.CompletedTask;
    });

    async Task Because() => _result = await _subscriber.OnNextPage(_events, new(null));

    [Fact] void should_handle_in_global_sequence_order() => _handled.ShouldEqual(new EventSequenceNumber[] { 1, 2, 3 });
    [Fact] void should_finish_at_the_last_event() => _result.LastSuccessfulObservation.ShouldEqual((EventSequenceNumber)3UL);
    [Fact] void should_batch_the_page_notifications() => _notifier.Received(1).NotifyMany(_key.Namespace, Arg.Any<IReadOnlyList<ReadModelChangeNotification>>());
    [Fact] void should_keep_run_keys_in_order() => _notifications.Select(_ => _.ReadModelKey).ShouldEqual(new ReadModelKey[] { "parent", "child", "parent" });
    [Fact] void should_keep_change_types_in_order() => _notifications.Select(_ => _.Change.ChangeType).ShouldEqual(_expectedChangeTypes);
    [Fact] void should_keep_event_positions_in_order() => _notifications.Select(_ => _.Change.EventSequenceNumber).ShouldEqual(new EventSequenceNumber[] { 1, 2, 3 });
    [Fact] void should_keep_each_runs_model() => _notifications.Select(_ => _.ReadModel[WellKnownProperties.LastHandledEventSequenceNumber]!.GetValue<ulong>()).ShouldEqual(new ulong[] { 1, 2, 3 });
    [Fact] void should_keep_event_timestamps() => _notifications.Select(_ => _.Change.Occurred).ShouldEqual(_events.Select(_ => _.Context.Occurred));
    [Fact] void should_keep_event_correlations() => _notifications.Select(_ => _.Change.CorrelationId).ShouldEqual(_events.Select(_ => _.Context.CorrelationId));
    [Fact] void should_not_notify_individual_runs() => _notifier.DidNotReceive().Notify(_key.Namespace, Arg.Any<ReadModelKey>(), Arg.Any<JsonObject>(), Arg.Any<ReadModelChangeContext>());
}
