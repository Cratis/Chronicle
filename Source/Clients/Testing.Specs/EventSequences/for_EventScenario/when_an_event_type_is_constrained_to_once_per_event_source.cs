// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

/// <summary>
/// A unique event type constraint is scoped to the event source, so a second occurrence is rejected for the holder
/// while another event source is still free to record its own first one. The harness has to agree with the kernel's
/// storage here or specs reject an append the real event store accepts.
/// </summary>
public class when_an_event_type_is_constrained_to_once_per_event_source : Specification, IDisposable
{
    static readonly EventSourceId _subscriber = EventSourceId.New();
    static readonly EventSourceId _anotherSubscriber = EventSourceId.New();

    EventScenario _scenario;
    AppendResult _firstActivation;
    AppendResult _secondActivation;
    AppendResult _activationForAnotherSubscriber;

    void Establish() => _scenario = new EventScenario();

    async Task Because()
    {
        _firstActivation = await _scenario.When
            .ForEventSource(_subscriber)
            .Events(new SubscriptionActivated("pro"));

        _secondActivation = await _scenario.When
            .ForEventSource(_subscriber)
            .Events(new SubscriptionActivated("pro"));

        _activationForAnotherSubscriber = await _scenario.When
            .ForEventSource(_anotherSubscriber)
            .Events(new SubscriptionActivated("pro"));
    }

    [Fact] void should_accept_the_first_activation() => _firstActivation.ShouldBeSuccessful();
    [Fact] void should_reject_a_second_activation_for_the_same_event_source() => _secondActivation.ShouldHaveConstraintViolation(SubscriptionActivated.ConstraintName);
    [Fact] void should_accept_a_first_activation_for_another_event_source() => _activationForAnotherSubscriber.ShouldBeSuccessful();

    public void Dispose() => _scenario.Dispose();
}
