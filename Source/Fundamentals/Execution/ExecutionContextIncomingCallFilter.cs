// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans;
using Orleans.Runtime;

namespace Cratis.Execution
{
    /// <summary>
    /// Represents an Orleans <see cref="IIncomingGrainCallFilter">grain call filter</see> that establishes
    /// the correct <see cref="ExecutionContext"/> based on values on the request context.
    /// </summary>
    public class ExecutionContextIncomingCallFilter : IIncomingGrainCallFilter
    {
        /// <inheritdoc/>
        public async Task Invoke(IIncomingGrainCallContext context)
        {
            if (context.ImplementationMethod?.DeclaringType?.Namespace?.StartsWith("Cratis", StringComparison.InvariantCulture) == true)
            {
                var tenantId = Guid.Parse(RequestContext.Get(RequestContextKeys.TenantId)?.ToString() ?? TenantId.NotSet.ToString());
                var correlationId = RequestContext.Get(RequestContextKeys.CorrelationId) as CorrelationId ?? string.Empty;

                ExecutionContextManager.SetCurrent(new ExecutionContext(tenantId, correlationId, string.Empty));
            }

            await context.Invoke();
        }
    }
}
