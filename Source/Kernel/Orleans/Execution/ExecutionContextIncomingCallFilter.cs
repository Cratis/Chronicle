// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Runtime;

namespace Cratis.Kernel.Orleans.Execution;

/// <summary>
/// Represents an Orleans <see cref="IIncomingGrainCallFilter">grain call filter</see> that establishes
/// the correct <see cref="ExecutionContext"/> based on values on the request context.
/// </summary>
public class ExecutionContextIncomingCallFilter : IIncomingGrainCallFilter
{
    readonly IExecutionContextManager _executionContextManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionContextIncomingCallFilter"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with execution context.</param>
    public ExecutionContextIncomingCallFilter(
        IExecutionContextManager executionContextManager)
    {
        _executionContextManager = executionContextManager;
    }

    /// <summary>
    /// Try to resolve execution context from the request context.
    /// </summary>
    /// <param name="context">Resolved context, default if not able to resolve.</param>
    /// <returns>True if it could resolve, false if not.</returns>
    public static bool TryResolveExecutionContext(out ExecutionContext context)
    {
        var microserviceId = RequestContext.Get(RequestContextKeys.MicroserviceId) as MicroserviceId;
        var tenantId = RequestContext.Get(RequestContextKeys.TenantId) as TenantId;
        var correlationId = RequestContext.Get(RequestContextKeys.CorrelationId) as CorrelationId;

        if (microserviceId is not null
            || tenantId is not null
            || correlationId is not null)
        {
            context = new ExecutionContext(
                microserviceId ?? MicroserviceId.Unspecified,
                tenantId ?? TenantId.NotSet,
                correlationId ?? CorrelationId.New());

            return true;
        }

        context = null!;
        return false;
    }

    /// <inheritdoc/>
    public async Task Invoke(IIncomingGrainCallContext context)
    {
        if (TryResolveExecutionContext(out var executionContext))
        {
            _executionContextManager.Set(executionContext);
        }

        await context.Invoke();
    }
}
