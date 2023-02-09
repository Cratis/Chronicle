// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.DependencyInversion;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Execution;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;

namespace Aksio.Cratis.Kernel.Grains.Observation.for_ObserverWorker;

public class ObserverWorkerImplementation : ObserverWorker
{
    MicroserviceId _microserviceId;
    TenantId _tenantId;
    EventSequenceId _eventSequenceId;
    MicroserviceId _sourceMicroserviceId;
    TenantId _sourceTenantId;

    protected override MicroserviceId MicroserviceId => _microserviceId;

    protected override TenantId TenantId => _tenantId;

    protected override EventSequenceId EventSequenceId => _eventSequenceId;

    protected override MicroserviceId? SourceMicroserviceId => _sourceMicroserviceId;

    protected override TenantId? SourceTenantId => _sourceTenantId;

    public ObserverWorkerImplementation(
        IExecutionContextManager executionContextManager,
        ProviderFor<IEventSequenceStorageProvider> eventSequenceStorageProviderProvider,
        IPersistentState<ObserverState> observerState,
        ILogger<ObserverWorker> logger)
        : base(executionContextManager, eventSequenceStorageProviderProvider, observerState, logger)
    {
    }

    public void SetMicroserviceId(MicroserviceId microserviceId) => _microserviceId = microserviceId;
    public void SetTenantId(TenantId tenantId) => _tenantId = tenantId;
    public void SetEventSequenceId(EventSequenceId eventSequenceId) => _eventSequenceId = eventSequenceId;
    public void SetSourceMicroserviceId(MicroserviceId sourceMicroserviceId) => _sourceMicroserviceId = sourceMicroserviceId;
    public void SetSourceTenantId(TenantId sourceTenantId) => _sourceTenantId = sourceTenantId;
    public void SetCurrentSubscription(ObserverSubscription subscription) => CurrentSubscription = subscription;
}

