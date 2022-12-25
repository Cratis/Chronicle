// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Execution.for_ExecutionContextManager;

public class when_creating_scope_for_tenant : Specification
{
    ExecutionContextManager manager;
    TenantId tenant_id;
    CorrelationId correlation_id;
    ExecutionContextScope scope;
    TenantId execution_context_tenant_id;
    CorrelationId execution_context_correlation_id;

    public when_creating_scope_for_tenant()
    {
         // Since the specification runner is using IAsyncLifetime - it will be in a different async context.
        // Use default behavior, since we need to have control over the async context.
        manager = new();

        tenant_id = Guid.NewGuid();
        correlation_id = CorrelationId.New();
        scope = manager.ForTenant(tenant_id, correlation_id);
        execution_context_tenant_id = manager.Current.TenantId;
        execution_context_correlation_id = manager.Current.CorrelationId;
    }

    [Fact] void should_create_a_scope_with_correct_tenant_id() => scope.TenantId.ShouldEqual(tenant_id);
    [Fact] void should_create_a_scope_with_correct_correlation_id() => scope.CorrelationId.ShouldEqual(correlation_id);
    [Fact] void should_set_correct_tenant_id_in_current_execution_context() => execution_context_tenant_id.ShouldEqual(tenant_id);
    [Fact] void should_set_correct_correlation_id_in_current_execution_context() => execution_context_correlation_id.ShouldEqual(correlation_id);
}
