// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber.when_a_child_is_removed_with_join;

public class and_a_parent_has_handled_a_later_event : given.a_run_with_affected_parents
{
    const ulong LaterSequenceNumber = 5000UL;
    IReadOnlyList<ReadModelChangeNotification> _notifications;
    bool _convertedWatermarkIsIntBacked;

    protected override JsonSchema ReadModelSchema() =>
        JsonSchema.FromJson($"{{\"type\":\"object\",\"properties\":{{\"fullName\":{{\"type\":\"string\"}},\"{WellKnownProperties.LastHandledEventSequenceNumber}\":{{\"type\":\"integer\"}}}}}}");

    void Establish()
    {
        ((IDictionary<string, object?>)_firstParentState)[WellKnownProperties.LastHandledEventSequenceNumber] = (int)LaterSequenceNumber;
        ((IDictionary<string, object?>)_secondParentState)[WellKnownProperties.LastHandledEventSequenceNumber] = (long)LaterSequenceNumber;
        var converter = new ExpandoObjectConverter(Substitute.For<ITypeFormats>());
        _expandoObjectConverter.ToJsonObject(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>())
            .Returns(call => converter.ToJsonObject(call.Arg<ExpandoObject>(), call.Arg<JsonSchema>()));
        _convertedWatermarkIsIntBacked = converter.ToJsonObject(_firstParentState, ReadModelSchema())[WellKnownProperties.LastHandledEventSequenceNumber] is JsonValue value && value.TryGetValue<int>(out _);
    }

    async Task Because()
    {
        _notifier.When(_ => _.NotifyMany(Arg.Any<EventStoreNamespaceName>(), Arg.Any<IReadOnlyList<ReadModelChangeNotification>>()))
            .Do(call => _notifications = call.Arg<IReadOnlyList<ReadModelChangeNotification>>());
        await _subscriber.OnNext("the-removed-group", [_event], new(null));
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_notify_both_affected_parents() => _notifications.Count.ShouldEqual(2);
    [Fact] void should_exercise_the_real_integer_backed_conversion() => _convertedWatermarkIsIntBacked.ShouldBeTrue();
    [Fact] void should_keep_the_higher_sequence_number_of_the_state() => _notifications.All(_ => _.ReadModel[WellKnownProperties.LastHandledEventSequenceNumber]!.GetValue<ulong>() == LaterSequenceNumber).ShouldBeTrue();
}
