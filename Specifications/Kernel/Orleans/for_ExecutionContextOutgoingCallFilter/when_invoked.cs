// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Execution;

namespace Aksio.Cratis.Kernel.Orleans.Execution.for_ExecutionContextOutgoingCallFilter;

public class when_invoked : Specification
{
    Mock<IExecutionContextManager> execution_context_manager;
    Mock<IRequestContextManager> request_context_manager;
    Mock<IOutgoingGrainCallContext> call_context;
    ExecutionContextOutgoingCallFilter filter;

    MicroserviceId microservice_id = Guid.Parse("06a4c7e6-8aa8-44af-ba68-b1b0093590e4");
    TenantId tenant_id = Guid.Parse("32a4fdd3-0a96-4a8e-aa42-95a45ac379b4");
    CorrelationId correlation_id = "21ece3b1-324a-4933-9d22-cd769d041fec";
    CausationId causation_id = "badc92a8-95d5-4df5-9e3e-9162b103f4c2";
    CausedBy caused_by = Guid.Parse("5e3bae13-7fd2-42e9-a72f-124588a200a9");

    void Establish()
    {
        execution_context_manager = new();
        request_context_manager = new();
        call_context = new();
        filter = new(execution_context_manager.Object, request_context_manager.Object);
        execution_context_manager.SetupGet(_ => _.IsInContext).Returns(true);
        execution_context_manager.SetupGet(_ => _.Current).Returns(new ExecutionContext(microservice_id, tenant_id, correlation_id, causation_id, caused_by));
    }

    Task Because() => filter.Invoke(call_context.Object);

    [Fact] void should_set_microservice_id() => request_context_manager.Verify(_ => _.Set(RequestContextKeys.MicroserviceId, microservice_id), Once);
    [Fact] void should_set_tenant_id() => request_context_manager.Verify(_ => _.Set(RequestContextKeys.TenantId, tenant_id), Once);
    [Fact] void should_set_correlation_id() => request_context_manager.Verify(_ => _.Set(RequestContextKeys.CorrelationId, correlation_id), Once);
    [Fact] void should_set_causation_id() => request_context_manager.Verify(_ => _.Set(RequestContextKeys.CausationId, causation_id), Once);
    [Fact] void should_set_caused_by_id() => request_context_manager.Verify(_ => _.Set(RequestContextKeys.CausedBy, caused_by), Once);
    [Fact] void should_forward_the_invoke() => call_context.Verify(_ => _.Invoke(), Once);
}
