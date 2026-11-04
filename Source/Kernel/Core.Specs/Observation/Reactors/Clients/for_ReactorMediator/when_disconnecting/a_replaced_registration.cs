// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_ReactorMediator.when_disconnecting;

public class a_replaced_registration : given.a_reactor_mediator_with_helpers
{
    void Establish()
    {
        Register(_first, _firstReplay);
        Register(_second, _secondReplay);
    }

    void Because()
    {
        _ownsRegistration = Disconnect(_first, _firstReplay);
        Deliver();
    }
    [Fact] void should_report_not_owned() => _ownsRegistration.ShouldBeFalse();
    [Fact] void should_keep_delivering_to_the_replacement() => _secondCalls.ShouldEqual(1);
    [Fact] void should_not_deliver_to_the_replaced_one() => _firstCalls.ShouldEqual(0);
}
