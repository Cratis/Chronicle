// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;
using Orleans;

namespace Aksio.Cratis.Extensions.Orleans.Execution.for_ExecutionContextIncomingCallFilter;

public class when_invoked_with_all_values_present : Specification
{
    Mock<IExecutionContextManager> execution_context_manager;
    Mock<IRequestContextManager> request_context_manager;
    Mock<IIncomingGrainCallContext> call_context;
    ExecutionContextIncomingCallFilter filter;

    MicroserviceId microservice_id = Guid.Parse("06a4c7e6-8aa8-44af-ba68-b1b0093590e4");
    TenantId tenant_id = Guid.Parse("32a4fdd3-0a96-4a8e-aa42-95a45ac379b4");
    CorrelationId correlation_id = "21ece3b1-324a-4933-9d22-cd769d041fec";
    CausationId causation_id = "badc92a8-95d5-4df5-9e3e-9162b103f4c2";
    CausedBy caused_by = Guid.Parse("5e3bae13-7fd2-42e9-a72f-124588a200a9");

    ExecutionContext execution_context;

    void Establish()
    {
        execution_context_manager = new();
        request_context_manager = new();
        call_context = new();
        filter = new(execution_context_manager.Object, request_context_manager.Object);

        request_context_manager.Setup(_ => _.Get(RequestContextKeys.MicroserviceId)).Returns(microservice_id);
        request_context_manager.Setup(_ => _.Get(RequestContextKeys.TenantId)).Returns(tenant_id);
        request_context_manager.Setup(_ => _.Get(RequestContextKeys.CorrelationId)).Returns(correlation_id);
        request_context_manager.Setup(_ => _.Get(RequestContextKeys.CausationId)).Returns(causation_id);
        request_context_manager.Setup(_ => _.Get(RequestContextKeys.CausedBy)).Returns(caused_by);

        execution_context_manager.Setup(_ => _.Set(IsAny<ExecutionContext>())).Callback((ExecutionContext ec) => execution_context = ec);
    }

    Task Because() => filter.Invoke(call_context.Object);

    [Fact] void should_hold_correct_microservice_id() => execution_context.MicroserviceId.ShouldEqual(microservice_id);
    [Fact] void should_hold_correct_tenant_id() => execution_context.TenantId.ShouldEqual(tenant_id);
    [Fact] void should_hold_correct_correlation_id() => execution_context.CorrelationId.ShouldEqual(correlation_id);
    [Fact] void should_hold_correct_causation_id() => execution_context.CausationId.ShouldEqual(causation_id);
    [Fact] void should_hold_correct_caused_by() => execution_context.CausedBy.ShouldEqual(caused_by);
}
