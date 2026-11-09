// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Reducers;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reducers.for_Reducers.when_registering_after_release;

public class and_a_handler_is_requested : when_registering_again_after_reconnect.given.a_connected_reducers
{
    Exception _typeError;
    Exception _genericError;

    void Establish() => _reducers.Release();

    async Task Because()
    {
        _typeError = await Catch.Exception(() => _reducers.Register(typeof(Second)));
        _genericError = await Catch.Exception(_reducers.Register<ThirdReducer, ThirdModel>);
    }

    [Fact] void should_refuse_the_type_registration() => _typeError.ShouldBeOfExactType<ObjectDisposedException>();
    [Fact] void should_refuse_the_generic_registration() => _genericError.ShouldBeOfExactType<ObjectDisposedException>();
    [Fact] void should_not_send_anything_to_the_kernel() => _services.Reducers.DidNotReceive().Observe(Arg.Any<IObservable<ReducerMessage>>(), Arg.Any<CallContext>());
}
