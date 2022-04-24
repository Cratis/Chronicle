// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;
using Orleans;
using Orleans.Runtime;

namespace Aksio.Cratis.Extensions.Orleans.Execution;

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
        var microserviceId = RequestContext.Get(RequestContextKeys.MicroserviceId);
        var tenantId = RequestContext.Get(RequestContextKeys.TenantId);
        var correlationId = RequestContext.Get(RequestContextKeys.CorrelationId);
        var causationId = RequestContext.Get(RequestContextKeys.CausationId);
        var causedBy = RequestContext.Get(RequestContextKeys.CausedBy);

        if (microserviceId is not null
            || tenantId is not null
            || correlationId is not null
            || causationId is not null
            || causedBy is not null)
        {
            context = new ExecutionContext(
                microserviceId?.ToString() ?? MicroserviceId.Unspecified,
                tenantId?.ToString() ?? TenantId.NotSet,
                correlationId?.ToString() ?? CorrelationId.New(),
                causationId?.ToString() ?? "[n/a]",
                causedBy?.ToString() ?? CausedBy.System);
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
