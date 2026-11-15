// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationLinkPump;

public class when_the_binding_is_removed : given.a_link_pump
{
    TimeSpan? _whileBound;
    TimeSpan? _afterRemoval;

    async Task Because()
    {
        _whileBound = await _pump.Step(default);
        _registry.GetBindings().Returns(_ => Task.FromResult<IReadOnlyList<FederationBindingState>>([]));
        _afterRemoval = await _pump.Step(default);
    }

    [Fact] void should_keep_running_while_bound() => _whileBound.ShouldNotBeNull();
    [Fact] void should_stop_after_removal() => _afterRemoval.ShouldBeNull();
    [Fact] void should_be_disabled() => _status.GetBinding(BindingId).State.ShouldEqual(FederationBindingStates.Disabled);
    [Fact] void should_close_the_connection() => _clients.Closed.ShouldEqual(1);
}
