// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Configuration;
using Aksio.Cratis.Events.Store.Grains.Connections;
using Aksio.Cratis.Events.Store.Observation;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Extensions.Orleans.Execution;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Aksio.Cratis.Events.Store.Grains.Observation.for_ClientObservers.given;

public class two_tenants_and_two_event_types : GrainSpecification<ClientObserversState>
{
    protected const string first_tenant = "a90218fb-9a00-4bc7-aa9b-7cc137ae6b91";
    protected const string second_tenant = "6582ba4b-1516-4578-bcbf-0dce555a156f";

    protected Mock<IExecutionContextManager> execution_context_manager;
    protected Mock<IRequestContextManager> request_context_manager;
    protected Tenants tenants;
    protected ExecutionContext execution_context;
    protected string connection_id;
    protected ObserverId observer_id = "a3535b94-e414-4c19-a2ce-b570fa9c913e";
    protected EventSequenceId event_sequence_id = "a3e80c03-8f4c-42ca-90ec-67fbea33e389";
    protected IEnumerable<EventType> event_types = new EventType[]
    {
        new("ad9f43ca-8d98-4669-99cd-dbd0eaaea9d9", 1),
        new("3e84ef60-c725-4b45-832d-29e3b663d7cd", 1)
    };

    protected Mock<IConnectedClients> connected_clients;
    protected Mock<IObserver> first_tenant_observer;
    protected Mock<IObserver> second_tenant_observer;

    protected ClientObservers observers;

    protected override Grain GetGrainInstance()
    {
        execution_context_manager = new();
        request_context_manager = new();
        tenants = new()
        {
            [first_tenant] = new(),
            [second_tenant] = new()
        };

        execution_context = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            CorrelationId.New(),
            CausationId.System,
            CausedBy.System
        );

        execution_context_manager.SetupGet(_ => _.Current).Returns(execution_context);

        connection_id = Guid.NewGuid().ToString();
        request_context_manager.Setup(_ => _.Get(RequestContextKeys.ConnectionId)).Returns(connection_id);

        return observers = new ClientObservers(
            execution_context_manager.Object,
            request_context_manager.Object,
            tenants,
            Mock.Of<ILogger<ClientObservers>>());
    }

    protected override void OnBeforeGrainActivate()
    {
        connected_clients = new();
        grain_factory.Setup(_ => _.GetGrain<IConnectedClients>(Guid.Empty, null)).Returns(connected_clients.Object);
    }

    void Establish()
    {
        first_tenant_observer = new();
        second_tenant_observer = new();

        grain_factory.Setup(_ => _.GetGrain<IObserver>(IsAny<Guid>(), IsAny<string>(), null)).Returns(
            (Guid _, string keyExtension, string __) =>
            {
                var observerKey = ObserverKey.Parse(keyExtension);
                if (observerKey.TenantId == first_tenant)
                {
                    return first_tenant_observer.Object;
                }

                if (observerKey.TenantId == second_tenant)
                {
                    return second_tenant_observer.Object;
                }

                return null;
            });
    }
}
