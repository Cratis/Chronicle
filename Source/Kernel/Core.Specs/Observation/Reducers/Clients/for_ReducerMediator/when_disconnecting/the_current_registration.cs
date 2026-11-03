// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ReducerMediator.when_disconnecting;

public class the_current_registration : given.a_reducer_mediator_with_helpers
{
    ObserverSubscriberState _state;

    void Establish() => Register(_first, _firstReplay);

    void Because()
    {
        _ownsRegistration = Disconnect(_first, _firstReplay);
        _state = Deliver();
    }
    [Fact] void should_report_owned() => _ownsRegistration.ShouldBeTrue();
    [Fact] void should_report_disconnected_on_delivery() => _state.ShouldEqual(ObserverSubscriberState.Disconnected);
}
