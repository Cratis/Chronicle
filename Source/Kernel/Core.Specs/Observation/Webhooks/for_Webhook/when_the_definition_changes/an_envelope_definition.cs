// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Webhooks.for_Webhook.when_the_definition_changes;

public class an_envelope_definition : given.a_webhook_grain_with_replay_on_definition_change
{
    IObserver _observer;

    async Task Establish()
    {
        _namespacesGrain.GetAll().Returns([new("namespace")]);
        _observer = Substitute.For<IObserver>();
        _silo.AddProbe(_ => _observer);
        await _grain.SetDefinition(_definition with { DeliveryFormat = WebhookDeliveryFormat.EnvelopeV1 });
    }

    async Task Because() => await _silo.TimerRegistry.FireAllAsync();

    [Fact] void should_not_register_a_replay_timer() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
    [Fact] async Task should_not_replay_the_namespace_observer() => await _observer.DidNotReceive().Replay();
}
