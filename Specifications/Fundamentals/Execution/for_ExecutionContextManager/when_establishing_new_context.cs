// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Execution
{
    public class when_establishing_new_context : Specification
    {
        ExecutionContextManager manager;
        TenantId tenant_id;
        CorrelationId correlation_id;

        void Establish()
        {
            manager = new();
            tenant_id = Guid.NewGuid();
            correlation_id = Guid.NewGuid().ToString();
        }

        void Because() => manager.Establish(tenant_id, correlation_id);

        [Fact] void should_have_the_current_context_with_tenant_id() => manager.Current.TenantId.ShouldEqual(tenant_id);
        [Fact] void should_have_the_current_context_with_correlation_id() => manager.Current.CorrelationId.ShouldEqual(correlation_id);
    }
}
