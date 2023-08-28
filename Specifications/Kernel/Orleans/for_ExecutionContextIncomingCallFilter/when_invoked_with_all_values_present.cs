// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Runtime;

namespace Aksio.Cratis.Kernel.Orleans.Execution.for_ExecutionContextIncomingCallFilter;

public class when_invoked_with_all_values_present : Specification
{
    Mock<IExecutionContextManager> execution_context_manager;
    Mock<IIncomingGrainCallContext> call_context;
    ExecutionContextIncomingCallFilter filter;

    MicroserviceId microservice_id = Guid.Parse("06a4c7e6-8aa8-44af-ba68-b1b0093590e4");
    TenantId tenant_id = Guid.Parse("32a4fdd3-0a96-4a8e-aa42-95a45ac379b4");
    CorrelationId correlation_id = "21ece3b1-324a-4933-9d22-cd769d041fec";

    ExecutionContext execution_context;

    void Establish()
    {
        execution_context_manager = new();
        call_context = new();
        filter = new(execution_context_manager.Object);

        execution_context_manager.Setup(_ => _.Set(IsAny<ExecutionContext>())).Callback((ExecutionContext ec) => execution_context = ec);
    }

    Task Because()
    {
        // We need to set the keys in the same async context as the filter will be invoked in
        RequestContext.Set(RequestContextKeys.MicroserviceId, microservice_id);
        RequestContext.Set(RequestContextKeys.TenantId, tenant_id);
        RequestContext.Set(RequestContextKeys.CorrelationId, correlation_id);

        return filter.Invoke(call_context.Object);
    }

    [Fact] void should_hold_correct_microservice_id() => execution_context.MicroserviceId.ShouldEqual(microservice_id);
    [Fact] void should_hold_correct_tenant_id() => execution_context.TenantId.ShouldEqual(tenant_id);
    [Fact] void should_hold_correct_correlation_id() => execution_context.CorrelationId.ShouldEqual(correlation_id);
}
