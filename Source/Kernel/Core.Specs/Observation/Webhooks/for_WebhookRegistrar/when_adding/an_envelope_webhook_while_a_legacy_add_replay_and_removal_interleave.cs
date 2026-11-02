// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.when_adding;

public class an_envelope_webhook_while_a_legacy_add_replay_and_removal_interleave : given.a_pinned_registration
{
    List<string> _steps;

    void Establish()
    {
        _definition.DeliveryFormat = Cratis.Chronicle.Contracts.Observation.Webhooks.WebhookDeliveryFormat.EnvelopeV1;
        _steps = [];
        _log.TryGetLastInstanceOfAny(Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<EventTypeId>>()).Returns(_ =>
        {
            _steps.Add("read lifecycle");
            return Task.FromResult(Cratis.Monads.Option<AppendedEvent>.None());
        });
        _storage.GetEventStore(Arg.Any<EventStoreName>()).Observers.Has(Arg.Any<ObserverId>()).Returns(_ =>
        {
            _steps.Add("check observer history");
            return false;
        });

        // Between the checks and the append a legacy add, a replay start and a removal happen, so the lifecycle moved on.
        TheScopedAppendFailsWithAConcurrencyViolation();
    }

    async Task Because() => _error = await Catch.Exception(() => _registrar.Add("store", [_definition]));

    [Fact] void should_read_the_lifecycle_before_checking_observer_history() => _steps.ShouldContainOnly("read lifecycle", "check observer history");
    [Fact] void should_check_observer_history_after_reading_the_lifecycle() => _steps.IndexOf("check observer history").ShouldBeGreaterThan(_steps.IndexOf("read lifecycle"));
    [Fact] void should_reject_the_envelope_registration() => _error.ShouldBeOfExactType<WebhookLifecycleChangedDuringRegistration>();
    [Fact] void should_append_against_the_position_read_first() => ScopedAppends.Single().SequenceNumber.ShouldEqual(EventSequenceNumber.BeforeFirst);
    [Fact] void should_not_create_the_webhook() => UnscopedAppendCount.ShouldEqual(0);
}
