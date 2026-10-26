// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.Reactors.for_Reactors;

public class when_released : given.a_registered_delegate
{
    IReactorHandler _afterDisconnect;
    CancellationToken _originalToken;
    int _registrationsBeforeRelease;

    async Task Establish()
    {
        await _reactors.Register();
        _registrationsBeforeRelease = _definitions.Count;
        _originalToken = _handler.CancellationToken;
    }

    async Task Because()
    {
        _reactors.Release();
        _connectionLifecycle.OnDisconnected += Raise.Event<Disconnected>();
        await _reactors.Register();
        _afterDisconnect = _reactors.GetHandlerById("bridge");
    }

    [Fact] void should_disconnect_the_handler() => _originalToken.IsCancellationRequested.ShouldBeTrue();
    [Fact] void should_not_recreate_the_handler() => ReferenceEquals(_afterDisconnect, _handler).ShouldBeTrue();
    [Fact] void should_not_register_again() => _definitions.Count.ShouldEqual(_registrationsBeforeRelease);
}
