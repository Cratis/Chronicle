// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Execution;

public class when_establishing_nested_contexts : Specification
{
    ExecutionContextManager manager;

    MicroserviceId first_microservice_id;
    TenantId first_tenant_id;
    CorrelationId first_correlation_id;

    MicroserviceId second_microservice_id;
    TenantId second_tenant_id;
    CorrelationId second_correlation_id;

    MicroserviceId root_microservice_id;
    TenantId root_tenant_id;
    CorrelationId root_correlation_id;

    MicroserviceId actual_first_microservice_id;
    TenantId actual_first_tenant_id;
    CorrelationId actual_first_correlation_id;

    public when_establishing_nested_contexts()
    {
        // Since the specification runner is using IAsyncLifetime - it will be in a different async context.
        // Use default behavior, since we need to have control over the async context.
        manager = new();
        root_microservice_id = Guid.NewGuid();
        root_tenant_id = Guid.NewGuid();
        root_correlation_id = Guid.NewGuid().ToString();
        first_microservice_id = Guid.NewGuid();
        first_tenant_id = Guid.NewGuid();
        first_correlation_id = Guid.NewGuid().ToString();
        second_microservice_id = Guid.NewGuid();
        second_tenant_id = Guid.NewGuid();
        second_correlation_id = Guid.NewGuid().ToString();
        manager.Establish(root_microservice_id, root_tenant_id, root_correlation_id);
    }

    void Because()
    {
        Task.Run(() =>
        {
            manager.Establish(first_microservice_id, first_tenant_id, first_correlation_id);
            Task.Run(() => manager.Establish(second_microservice_id, second_tenant_id, second_correlation_id)).Wait();
            actual_first_microservice_id = manager.Current.MicroserviceId;
            actual_first_tenant_id = manager.Current.TenantId;
            actual_first_correlation_id = manager.Current.CorrelationId;
        }).Wait();
    }

    [Fact] void should_keep_root_tenant_when_in_root_async_context() => manager.Current.TenantId.ShouldEqual(root_tenant_id);
    [Fact] void should_keep_root_correlation_when_in_root_async_context() => manager.Current.CorrelationId.ShouldEqual(root_correlation_id);
    [Fact] void should_keep_actual_first_tenant_when_in_first_async_context() => actual_first_tenant_id.ShouldEqual(first_tenant_id);
    [Fact] void should_keep_actual_first_correlation_when_in_first_async_context() => actual_first_correlation_id.ShouldEqual(first_correlation_id);
}
