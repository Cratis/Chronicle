// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Execution.for_ExecutionContextManager;

public class when_scope_for_tenant_goes_out_of_scope : Specification
{
    ExecutionContextManager manager;

    public when_scope_for_tenant_goes_out_of_scope()
    {
         // Since the specification runner is using IAsyncLifetime - it will be in a different async context.
        // Use default behavior, since we need to have control over the async context.
        manager = new();

        var scope = manager.ForTenant(Guid.NewGuid());
        scope.Dispose();
    }

    [Fact] void should_have_execution_context_without_tenant_set() => manager.Current.TenantId.ShouldEqual(TenantId.NotSet);
}
