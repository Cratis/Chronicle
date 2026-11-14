// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber.when_a_child_is_removed_with_join;

public class and_a_parent_has_handled_a_later_event : given.a_run_with_affected_parents
{
    const ulong LaterSequenceNumber = 5000UL;
    IReadOnlyList<ReadModelChangeNotification> _notifications;

    protected override JsonSchema ReadModelSchema() =>
        JsonSchema.FromJson($"{{\"type\":\"object\",\"properties\":{{\"fullName\":{{\"type\":\"string\"}},\"{WellKnownProperties.LastHandledEventSequenceNumber}\":{{\"type\":\"integer\"}}}}}}");

    void Establish() =>
        _expandoObjectConverter.ToJsonObject(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>())
            .Returns(_ => new JsonObject { [WellKnownProperties.LastHandledEventSequenceNumber] = LaterSequenceNumber });

    async Task Because()
    {
        _notifier.When(_ => _.NotifyMany(Arg.Any<EventStoreNamespaceName>(), Arg.Any<IReadOnlyList<ReadModelChangeNotification>>()))
            .Do(call => _notifications = call.Arg<IReadOnlyList<ReadModelChangeNotification>>());
        await _subscriber.OnNext("the-removed-group", [_event], new(null));
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_keep_the_higher_sequence_number_of_the_state() => _notifications.All(_ => _.ReadModel[WellKnownProperties.LastHandledEventSequenceNumber]!.GetValue<ulong>() == LaterSequenceNumber).ShouldBeTrue();
}
