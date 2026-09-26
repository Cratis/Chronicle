// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Reactors;

namespace Cratis.Chronicle.Reactors.for_Reactors;

public class when_unregistering_a_delegate : given.a_registered_delegate
{
    CancellationToken _token;
    Exception _lookupException;

    void Establish()
    {
        _token = _handler.CancellationToken;
        _services.Reactors.HasReactor(Arg.Any<HasReactorRequest>()).Returns(new HasReactorResponse { Exists = false });
    }

    void Because()
    {
        _reactors.Unregister("bridge");
        _lookupException = Catch.Exception(() => _reactors.GetHandlerById("bridge"));
    }

    [Fact] void should_cancel_the_observation() => _token.IsCancellationRequested.ShouldBeTrue();
    [Fact] void should_not_return_the_local_handler_by_id() => _lookupException.ShouldBeOfExactType<UnknownReactorId>();
}
