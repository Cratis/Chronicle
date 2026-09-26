// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Identities;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_delivering_to_a_delegate;

public class and_identity_and_causation_are_set : given.a_registered_delegate
{
    IDictionary<string, string> _causation;
    Identity _identity;

    void Establish()
    {
        _causationManager.When(_ => _.BeginScope(ReactorHandler.CausationType, Arg.Any<IDictionary<string, string>>()))
            .Do(call => _causation = call.Arg<IDictionary<string, string>>());
        _identityProvider.When(_ => _.SetCurrentIdentity(Arg.Any<Identity>()))
            .Do(call => _identity = call.Arg<Identity>());
    }

    async Task Because()
    {
        _observed.OnNext(new Contracts.Observation.EventsToObserve
        {
            Partition = "order-42",
            Events = [new Contracts.Events.AppendedEvent
            {
                Context = (EventContext.Empty with
                {
                    EventType = new EventType("orders", 1),
                    SequenceNumber = 12,
                    CausedBy = new Identity("user-subject", "User", "user")
                }).ToContract(),
                Content = "{\"order\":42}"
            }]
        });
        await _received.Task.WaitAsync(TimeSpan.FromSeconds(10));
        _release.SetResult();
        await _result.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact] void should_set_system_identity_on_behalf_of_the_originator() => _identity.OnBehalfOf.Subject.ShouldEqual("user-subject");
    [Fact] void should_record_the_reactor_id() => _causation[ReactorHandler.CausationReactorIdProperty].ShouldEqual("bridge");
    [Fact] void should_record_the_delivered_generation() => _causation[ReactorHandler.CausationEventTypeGenerationProperty].ShouldEqual("1");
    [Fact] void should_record_the_sequence_number() => _causation[ReactorHandler.CausationEventSequenceNumberProperty].ShouldEqual("12");
    [Fact] void should_clear_current_identity() => _identityProvider.Received(1).ClearCurrentIdentity();
}
