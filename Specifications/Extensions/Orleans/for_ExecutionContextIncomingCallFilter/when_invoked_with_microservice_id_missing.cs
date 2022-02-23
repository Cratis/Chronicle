// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;
using Orleans;

namespace Aksio.Cratis.Extensions.Orleans.Execution.for_ExecutionContextIncomingCallFilter;

public class when_invoked_with_microservice_id_missing : Specification
{
    Mock<IExecutionContextManager> execution_context_manager;
    Mock<IRequestContextManager> request_context_manager;
    Mock<IIncomingGrainCallContext> call_context;
    ExecutionContextIncomingCallFilter filter;

    TenantId tenant_id = Guid.Parse("32a4fdd3-0a96-4a8e-aa42-95a45ac379b4");
    CorrelationId correlation_id = "21ece3b1-324a-4933-9d22-cd769d041fec";
    CausationId causation_id = "badc92a8-95d5-4df5-9e3e-9162b103f4c2";
    CausedBy caused_by = Guid.Parse("5e3bae13-7fd2-42e9-a72f-124588a200a9");

    Exception result;

    void Establish()
    {
        execution_context_manager = new();
        request_context_manager = new();
        call_context = new();
        filter = new(execution_context_manager.Object, request_context_manager.Object);

        request_context_manager.Setup(_ => _.Get(RequestContextKeys.TenantId)).Returns(tenant_id);
        request_context_manager.Setup(_ => _.Get(RequestContextKeys.CorrelationId)).Returns(correlation_id);
        request_context_manager.Setup(_ => _.Get(RequestContextKeys.CausationId)).Returns(causation_id);
        request_context_manager.Setup(_ => _.Get(RequestContextKeys.CausedBy)).Returns(caused_by);
    }

    async Task Because() => result = await Catch.Exception(async () => await filter.Invoke(call_context.Object));

    [Fact] void should_throw_unable_to_establish_execution_context() => result.ShouldBeOfExactType<UnableToEstablishExecutionContextFromRequestContext>();
}
