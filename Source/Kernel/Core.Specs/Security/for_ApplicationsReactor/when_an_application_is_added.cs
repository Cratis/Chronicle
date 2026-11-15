// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Security;
using StorageApplication = Cratis.Chronicle.Storage.Security.Application;

namespace Cratis.Chronicle.Security.for_ApplicationsReactor;

public class when_an_application_is_added : Specification
{
    static readonly EventSourceId _applicationId = Guid.NewGuid();

    IApplicationStorage _applications;
    ApplicationsReactor _reactor;

    void Establish()
    {
        _applications = Substitute.For<IApplicationStorage>();
        _applications.GetById(Arg.Any<Concepts.Security.ApplicationId>()).Returns((StorageApplication?)null);
        _reactor = new ApplicationsReactor(_applications);
    }

    Task Because() => _reactor.Added(new ApplicationAdded("client", "secret-hash"), EventContext.Empty with { EventSourceId = _applicationId });

    [Fact] void should_create_the_application() => _applications.Received(1).Create(Arg.Is<StorageApplication>(application => application.Id.Value == Guid.Parse(_applicationId.Value) && application.ClientId == "client"));
    [Fact] void should_not_update_an_application() => _applications.DidNotReceive().Update(Arg.Any<StorageApplication>());
}
