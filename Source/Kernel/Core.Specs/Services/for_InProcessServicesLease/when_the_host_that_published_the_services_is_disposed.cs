// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.for_InProcessServicesLease;

[Collection("SystemStoreGuard")]
public class when_the_host_that_published_the_services_is_disposed : Specification
{
    IServiceProvider _previous;
    IServiceProvider _published;
    InProcessServicesLease _lease;

    void Establish()
    {
        _previous = SystemStoreGuard.InProcessServices;
        _published = Substitute.For<IServiceProvider>();
        _lease = new();
        _lease.Publish(_published);
    }

    void Because() => _lease.Dispose();

    void Destroy() => SystemStoreGuard.InProcessServices = _previous;

    [Fact] void should_no_longer_publish_the_services() => SystemStoreGuard.InProcessServices.ShouldBeNull();
}
