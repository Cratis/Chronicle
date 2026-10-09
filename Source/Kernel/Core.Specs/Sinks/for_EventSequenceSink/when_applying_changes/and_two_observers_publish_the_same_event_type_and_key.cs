// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Sinks.for_EventSequenceSink.when_applying_changes;

/// <summary>
/// Two observers misconfigured to publish the same event type and key to one destination. Registration refuses it,
/// but the sink must not fold the other's state even if it is reached.
/// </summary>
public class and_two_observers_publish_the_same_event_type_and_key : given.a_sink
{
    ExpandoObject? _secondSees;
    ExpandoObject? _firstSees;
    ExpandoObject? _otherObserverState;

    async Task Because()
    {
        var second = new EventSequenceSink(
            Store,
            Tenant,
            _definition with { ObserverIdentifier = "other-observer", Identifier = "other-totals" },
            _configuration,
            _destinations.Grains,
            _destinations.Storage,
            _destinations.Converter);

        await _sink.ApplyChanges("customer-1", Fold(new(), 10, 1, 5), 5);
        _secondSees = await second.FindOrDefault("customer-1");
        await second.ApplyChanges("customer-1", Fold(new(), 99, 1, 6), 6);
        await _sink.ApplyChanges("customer-1", Fold(await _sink.FindOrDefault("customer-1") ?? new(), 20, 2, 7), 7);

        _firstSees = await _sink.FindOrDefault("customer-1");
        _otherObserverState = await second.FindOrDefault("customer-1");
    }

    [Fact] void should_not_let_the_second_observer_see_the_first_ones_state() => _secondSees.ShouldBeNull();
    [Fact] void should_keep_each_observers_own_last_state() => TotalOf(_otherObserverState).ShouldEqual("99");
    [Fact] void should_fold_the_first_observer_from_its_own_state_only() => TotalOf(_firstSees).ShouldEqual("20");
    [Fact] void should_publish_every_step_as_its_own_event() => _destinations.Events(Store, Tenant, EventSequenceId.Outbox).Count.ShouldEqual(3);
}
