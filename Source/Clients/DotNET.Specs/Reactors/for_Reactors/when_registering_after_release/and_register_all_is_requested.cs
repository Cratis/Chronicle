// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Reactors;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_registering_after_release;

public class and_register_all_is_requested : given.all_dependencies
{
    void Establish()
    {
        _reactors.Release();
        _servicesAccessor.ClearReceivedCalls();
    }

    Task Because() => _reactors.Register();

    [Fact] void should_not_send_a_registration() => _services.Reactors.DidNotReceive().Observe(Arg.Any<IObservable<ReactorMessage>>(), Arg.Any<CallContext>());
    [Fact] void should_not_resolve_services() => _ = _servicesAccessor.DidNotReceive().Services;
}
