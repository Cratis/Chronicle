// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_ReactorMediator.when_disconnecting;

public class an_already_removed_registration : given.a_reactor_mediator_with_helpers
{
    void Establish()
    {
        Register(_first, _firstReplay);
        Disconnect(_first, _firstReplay);
    }

    void Because() => _ownsRegistration = Disconnect(_first, _firstReplay);
    [Fact] void should_report_owned() => _ownsRegistration.ShouldBeTrue();
}
