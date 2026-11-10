// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Security;
using StorageApplication = Cratis.Chronicle.Storage.Security.Application;

namespace Cratis.Chronicle.Security.for_ApplicationsReactor;

public class when_an_application_is_added_again : Specification
{
    static readonly EventSourceId _applicationId = Guid.NewGuid();

    IApplicationStorage _applications;
    ApplicationsReactor _reactor;

    void Establish()
    {
        _applications = Substitute.For<IApplicationStorage>();
        _applications.GetById(Arg.Any<Concepts.Security.ApplicationId>()).Returns(new StorageApplication { Id = Guid.Parse(_applicationId.Value), ClientId = "client", ClientSecret = "first-hash" });
        _reactor = new ApplicationsReactor(_applications);
    }

    Task Because() => _reactor.Added(new ApplicationAdded("client", "second-hash"), EventContext.Empty with { EventSourceId = _applicationId });

    [Fact] void should_not_create_a_second_application() => _applications.DidNotReceive().Create(Arg.Any<StorageApplication>());
    [Fact] void should_update_the_existing_application() => _applications.Received(1).Update(Arg.Is<StorageApplication>(application => application.Id.Value == Guid.Parse(_applicationId.Value) && application.ClientSecret == "second-hash"));
}
