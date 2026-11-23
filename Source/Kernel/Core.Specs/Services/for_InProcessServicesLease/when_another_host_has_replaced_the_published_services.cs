// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.for_InProcessServicesLease;

[Collection("SystemStoreGuard")]
public class when_another_host_has_replaced_the_published_services : Specification
{
    IServiceProvider _previous;
    IServiceProvider _replacement;
    InProcessServicesLease _lease;

    void Establish()
    {
        _previous = SystemStoreGuard.InProcessServices;
        _lease = new();
        _lease.Publish(Substitute.For<IServiceProvider>());
        _replacement = Substitute.For<IServiceProvider>();
        SystemStoreGuard.InProcessServices = _replacement;
    }

    void Because() => _lease.Dispose();

    void Destroy() => SystemStoreGuard.InProcessServices = _previous;

    [Fact] void should_leave_the_replacement_published() => SystemStoreGuard.InProcessServices.ShouldEqual(_replacement);
}
