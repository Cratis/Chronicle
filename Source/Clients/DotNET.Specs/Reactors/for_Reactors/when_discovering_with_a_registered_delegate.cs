// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.for_Reactors;

public class when_discovering_with_a_registered_delegate : given.a_registered_delegate
{
    CancellationToken _originalToken;
    IReactorHandler _discovered;

    void Establish() => _originalToken = _handler.CancellationToken;

    async Task Because()
    {
        await _reactors.Discover();
        await _reactors.Register();
        _discovered = _reactors.GetHandlerById("bridge");
    }

    [Fact] void should_disconnect_the_old_handler() => _originalToken.IsCancellationRequested.ShouldBeTrue();
    [Fact] void should_recreate_the_delegate_handler() => ReferenceEquals(_discovered, _handler).ShouldBeFalse();
    [Fact] void should_register_the_delegate_again() => _definitions.Count.ShouldEqual(2);
}
