// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Projections.for_ProjectionChangesetNotifier;

public class when_notifying_many : given.a_subscribed_notifier
{
    async Task Because() => await _notifier.NotifyMany(_namespace, _notifications);

    [Fact]
    void should_deliver_each_change_to_each_subscriber_in_order()
    {
        foreach (var subscriber in _subscribers)
        {
            Received.InOrder(() =>
            {
                foreach (var notification in _notifications)
                {
                    subscriber.OnChangeset(_namespace, notification.ReadModelKey, notification.ReadModel, notification.Change);
                }
            });
        }
    }
    [Fact] void should_notify_the_first_subscriber_once_per_item() => _subscribers[0].Received(3).OnChangeset(_namespace, Arg.Any<ReadModelKey>(), Arg.Any<JsonObject>(), Arg.Any<ReadModelChangeContext>());
    [Fact] void should_notify_the_second_subscriber_once_per_item() => _subscribers[1].Received(3).OnChangeset(_namespace, Arg.Any<ReadModelKey>(), Arg.Any<JsonObject>(), Arg.Any<ReadModelChangeContext>());
}
