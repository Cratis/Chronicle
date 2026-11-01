// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Sequences;

namespace Cratis.Chronicle.Connections.for_LazyServices.when_getting_a_service;

public class for_the_first_time : given.a_lazy_services
{
    IEventSequences _result;

    void Because() => _result = _services.Sequences;

    [Fact] void should_create_it_once() => _factory.Creations.Count.ShouldEqual(1);
    [Fact] void should_return_the_created_service() => ReferenceEquals(_result, _factory.Creations.Single().Service).ShouldBeTrue();
    [Fact] void should_use_the_connections_call_invoker() => ReferenceEquals(_factory.Creations.Single().CallInvoker, _callInvoker).ShouldBeTrue();
    [Fact] void should_create_the_requested_type() => _factory.Creations.Single().ServiceType.ShouldEqual(typeof(IEventSequences));
}
