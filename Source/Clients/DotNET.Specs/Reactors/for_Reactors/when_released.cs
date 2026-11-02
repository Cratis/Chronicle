// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.Reactors.for_Reactors;

public class when_released : given.a_registered_delegate
{
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
    }

    [Fact] void should_disconnect_the_handler() => _originalToken.IsCancellationRequested.ShouldBeTrue();
    [Fact] void should_not_retain_or_recreate_any_handler() => _reactors.Handlers.ShouldBeEmpty();
    [Fact] void should_not_register_again() => _definitions.Count.ShouldEqual(_registrationsBeforeRelease);
}
