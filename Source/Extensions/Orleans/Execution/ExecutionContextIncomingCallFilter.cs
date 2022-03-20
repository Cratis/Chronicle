// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;
using Orleans;

namespace Aksio.Cratis.Extensions.Orleans.Execution;

/// <summary>
/// Represents an Orleans <see cref="IIncomingGrainCallFilter">grain call filter</see> that establishes
/// the correct <see cref="ExecutionContext"/> based on values on the request context.
/// </summary>
public class ExecutionContextIncomingCallFilter : IIncomingGrainCallFilter
{
    static readonly string[] _nonExecutionContextTypes = new[]
    {
        "IMembershipTable",
        "IPersistentStreamPullingManager",
        "IPersistentStreamPullingAgent",
        "IDeploymentLoadPublisher"
    };

    readonly IExecutionContextManager _executionContextManager;
    readonly IRequestContextManager _requestContextManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionContextIncomingCallFilter"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with execution context.</param>
    /// <param name="requestContextManager"><see cref="IRequestContextManager"/> for working with state in requests.</param>
    public ExecutionContextIncomingCallFilter(
        IExecutionContextManager executionContextManager,
        IRequestContextManager requestContextManager)
    {
        _executionContextManager = executionContextManager;
        _requestContextManager = requestContextManager;
    }

    /// <inheritdoc/>
    public async Task Invoke(IIncomingGrainCallContext context)
    {
        if (IsExecutionContextRequired(context))
        {
            var microserviceId = _requestContextManager.Get(RequestContextKeys.MicroserviceId);
            var tenantId = _requestContextManager.Get(RequestContextKeys.TenantId);
            if (microserviceId == null || tenantId == null)
            {
                throw new UnableToEstablishExecutionContextFromRequestContext();
            }

            var correlationId = _requestContextManager.Get(RequestContextKeys.CorrelationId) ?? "[N/A]";
            var causationId = _requestContextManager.Get(RequestContextKeys.CausationId)?.ToString() ?? "[N/A]";
            var causedBy = Guid.Parse(_requestContextManager.Get(RequestContextKeys.CausedBy)?.ToString() ?? Guid.Empty.ToString());
            _executionContextManager.Set(new ExecutionContext(microserviceId!.ToString()!, tenantId!.ToString()!, correlationId!.ToString()!, causationId, causedBy));
        }

        await context.Invoke();
    }

    bool IsExecutionContextRequired(IIncomingGrainCallContext context) => !_nonExecutionContextTypes.Any(_ => context.InterfaceMethod.DeclaringType?.Name.Equals(_, StringComparison.InvariantCulture) ?? false);
}
