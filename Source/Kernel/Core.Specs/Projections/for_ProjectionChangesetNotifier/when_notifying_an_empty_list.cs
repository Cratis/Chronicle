// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Projections.for_ProjectionChangesetNotifier;

public class when_notifying_an_empty_list : given.a_subscribed_notifier
{
    async Task Because() => await _notifier.NotifyMany(_namespace, []);

    [Fact] void should_not_notify_the_first_subscriber() => _subscribers[0].DidNotReceive().OnChangeset(_namespace, Arg.Any<ReadModelKey>(), Arg.Any<JsonObject>(), Arg.Any<ReadModelChangeContext>());
    [Fact] void should_not_notify_the_second_subscriber() => _subscribers[1].DidNotReceive().OnChangeset(_namespace, Arg.Any<ReadModelKey>(), Arg.Any<JsonObject>(), Arg.Any<ReadModelChangeContext>());
}
