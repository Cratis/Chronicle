// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.for_Reactors;

public class when_unregistering_a_delegate : given.a_registered_delegate
{
    CancellationToken _token;

    void Establish() => _token = _handler.CancellationToken;

    void Because() => _reactors.Unregister("bridge");

    [Fact] void should_cancel_the_observation() => _token.IsCancellationRequested.ShouldBeTrue();
}
