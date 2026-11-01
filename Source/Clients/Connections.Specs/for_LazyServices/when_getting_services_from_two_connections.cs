// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Grpc.Core;

namespace Cratis.Chronicle.Connections.for_LazyServices;

public class when_getting_services_from_two_connections : given.a_lazy_services
{
    CallInvoker _otherInvoker;
    IServices _otherServices;

    void Establish()
    {
        _otherInvoker = Substitute.For<CallInvoker>();
        _otherServices = new LazyServices(_otherInvoker, _factory);
    }

    void Because()
    {
        _ = _otherServices.Sequences;
        _ = _services.Sequences;
    }

    [Fact] void should_bind_the_new_container_to_its_invoker() => ReferenceEquals(_factory.Creations.First().CallInvoker, _otherInvoker).ShouldBeTrue();
    [Fact] void should_keep_late_getters_on_the_old_containers_invoker() => ReferenceEquals(_factory.Creations.Last().CallInvoker, _callInvoker).ShouldBeTrue();
    [Fact] void should_not_share_proxies() => ReferenceEquals(_services.Sequences, _otherServices.Sequences).ShouldBeFalse();
}
