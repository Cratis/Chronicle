// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Kernel.Orleans.Execution;

/// <summary>
/// Represents an Orleans <see cref="IOutgoingGrainCallFilter">grain call filter</see> that adds
/// the correct <see cref="ExecutionContext"/> values to the request context.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ExecutionContextOutgoingCallFilter"/> class.
/// </remarks>
/// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with execution context.</param>
/// <param name="requestContextManager"><see cref="IRequestContextManager"/> for working with state in requests.</param>
public class ExecutionContextOutgoingCallFilter(
    IExecutionContextManager executionContextManager,
    IRequestContextManager requestContextManager) : IOutgoingGrainCallFilter
{
    /// <inheritdoc/>
    public async Task Invoke(IOutgoingGrainCallContext context)
    {
        if (executionContextManager.IsInContext)
        {
            var executionContext = executionContextManager.Current;
            requestContextManager.Set(RequestContextKeys.MicroserviceId, executionContext.MicroserviceId);
            requestContextManager.Set(RequestContextKeys.TenantId, executionContext.TenantId);
            requestContextManager.Set(RequestContextKeys.CorrelationId, executionContext.CorrelationId);
        }
        else
        {
            requestContextManager.Set(RequestContextKeys.MicroserviceId, ExecutionContextManager.GlobalMicroserviceId);
        }
        await context.Invoke();
    }
}
