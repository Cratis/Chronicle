// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_ChronicleClient.when_opening_foreign_stores.given;

public class a_client_with_an_own_store : when_opening_namespace_stores.given.a_client_with_seeds
{
    protected IEventStore _own;
    protected IEventStore _foreign;
    protected int _ownReadModelRegistrations;

    async Task Establish()
    {
        _own = await _client.GetEventStore("own");
        _ownReadModelRegistrations = _services.ReadModels.ReceivedCalls().Count();
        ClearRegistrationRequests();
    }

    protected void ClearRegistrationRequests()
    {
        _services.EventStores.ClearReceivedCalls();
        _services.EventTypes.ClearReceivedCalls();
        _services.ReadModels.ClearReceivedCalls();
        _services.Projections.ClearReceivedCalls();
        _services.Reactors.ClearReceivedCalls();
        _services.Reducers.ClearReceivedCalls();
        _services.Constraints.ClearReceivedCalls();
        _services.Seeding.ClearReceivedCalls();
        _services.EventStoreSubscriptions.ClearReceivedCalls();
    }

    protected void ShouldNotRegisterForeignArtifacts()
    {
        _services.ReadModels.ReceivedCalls().ShouldBeEmpty();
        _services.Projections.ReceivedCalls().ShouldBeEmpty();
        _services.Reactors.ReceivedCalls().ShouldBeEmpty();
        _services.Reducers.ReceivedCalls().ShouldBeEmpty();
        _services.Constraints.ReceivedCalls().ShouldBeEmpty();
        _services.Seeding.ReceivedCalls().ShouldBeEmpty();
        _services.EventStoreSubscriptions.ReceivedCalls().ShouldBeEmpty();
    }
}
