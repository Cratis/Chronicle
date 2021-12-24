// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;
using Orleans;

namespace Cratis.Extensions.Orleans.Execution
{
    /// <summary>
    /// Represents an Orleans <see cref="IOutgoingGrainCallFilter">grain call filter</see> that adds
    /// the correct <see cref="ExecutionContext"/> values to the request context.
    /// </summary>
    public class ExecutionContextOutgoingCallFilter : IOutgoingGrainCallFilter
    {
        readonly IExecutionContextManager _executionContextManager;
        readonly IRequestContextManager _requestContextManager;

        public ExecutionContextOutgoingCallFilter(
            IExecutionContextManager executionContextManager,
            IRequestContextManager requestContextManager)
        {
            _executionContextManager = executionContextManager;
            _requestContextManager = requestContextManager;
        }

        /// <inheritdoc/>
        public async Task Invoke(IOutgoingGrainCallContext context)
        {
            if (_executionContextManager.IsInContext)
            {
                var executionContext = _executionContextManager.Current;
                _requestContextManager.Set(RequestContextKeys.TenantId, executionContext.TenantId.ToString());
                _requestContextManager.Set(RequestContextKeys.CorrelationId, executionContext.CorrelationId);
            }
            await context.Invoke();
        }
    }
}
