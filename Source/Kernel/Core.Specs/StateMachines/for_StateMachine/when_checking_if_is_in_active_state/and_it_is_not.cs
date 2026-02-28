// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.StateMachines.when_checking_if_is_in_active_state;

public class and_it_is_not : given.a_state_machine_with_well_known_states
{
    bool _result;

    void Because() => _result = StateMachine.IsInActiveState;

    [Fact] void should_not_be_in_state() => _result.ShouldBeFalse();
}
