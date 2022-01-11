// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;
using Orleans;

namespace Cratis.Extensions.Orleans.Execution
{
    /// <summary>
    /// Represents an Orleans <see cref="IIncomingGrainCallFilter">grain call filter</see> that establishes
    /// the correct <see cref="ExecutionContext"/> based on values on the request context.
    /// </summary>
    public class ExecutionContextIncomingCallFilter : IIncomingGrainCallFilter
    {
        readonly IRequestContextManager _requestContextManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionContextIncomingCallFilter"/> class.
        /// </summary>
        /// <param name="requestContextManager"><see cref="IRequestContextManager"/> for working with state in requests.</param>
        public ExecutionContextIncomingCallFilter(IRequestContextManager requestContextManager)
        {
            _requestContextManager = requestContextManager;
        }

        /// <inheritdoc/>
        public async Task Invoke(IIncomingGrainCallContext context)
        {
            var tenantId = _requestContextManager.Get(RequestContextKeys.TenantId);
            if (tenantId != null)
            {
                var correlationId = _requestContextManager.Get(RequestContextKeys.CorrelationId) ?? "[N/A]";
                ExecutionContextManager.SetCurrent(new ExecutionContext(tenantId!.ToString()!, correlationId!.ToString()!, string.Empty, Guid.Empty));
            }

            await context.Invoke();
        }
    }
}
