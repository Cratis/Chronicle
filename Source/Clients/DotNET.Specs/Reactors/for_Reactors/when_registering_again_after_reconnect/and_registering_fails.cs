// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Reactors;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_registering_again_after_reconnect;

public class and_registering_fails : given.a_registered_delegate_that_was_disconnected
{
    readonly Exception _failure = new("registration failed");
    Exception _error;
    bool _fail;

    void Establish()
    {
        _fail = true;
        _services.Reactors.When(_ => _.Observe(Arg.Any<IObservable<ReactorMessage>>(), Arg.Any<CallContext>()))
            .Do(_ =>
            {
                if (_fail)
                {
                    throw _failure;
                }
            });
    }

    async Task Because()
    {
        _error = await Catch.Exception(_reactors.RegisterAgainAfterReconnect);
        _fail = false;
        await _reactors.RegisterAgainAfterReconnect();
    }

    [Fact] void should_throw() => _error.ShouldEqual(_failure);
    [Fact] void should_register_on_a_later_call() => _definitions.Count.ShouldEqual(2);
}
