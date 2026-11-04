// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Projections.for_ProjectionChangesetNotifier.when_subscribing;

public class and_the_subscriber_is_already_registered : given.a_subscribed_notifier
{
    async Task Because()
    {
        await _notifier.Subscribe(_subscribers[0]);
        await _notifier.Notify(_namespace, "key", new JsonObject(), new ReadModelChangeContext(ReadModelChangeType.Added, 1, DateTimeOffset.UnixEpoch, CorrelationId.New()));
    }

    [Fact] void should_keep_a_single_registration() => _subscribers[0].Received(1).OnChangeset(_namespace, Arg.Any<ReadModelKey>(), Arg.Any<JsonObject>(), Arg.Any<ReadModelChangeContext>());
}
