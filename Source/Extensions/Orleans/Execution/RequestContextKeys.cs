// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Extensions.Orleans.Execution
{
    /// <summary>
    /// Holds all the keys used in the Orleans Request Context.
    /// </summary>
    /// <remarks>
    /// See <see cref="ExecutionContextOutgoingCallFilter"/> and <see cref="ExecutionContextIncomingCallFilter"/> for usage.
    /// </remarks>
    public static class RequestContextKeys
    {
        /// <summary>
        /// The tenant identifier key.
        /// </summary>
        public const string TenantId = "TenantId";

        /// <summary>
        /// The correlation identifier key.
        /// </summary>
        public const string CorrelationId = "CorrelationId";

        /// <summary>
        /// The causation identifier key.
        /// </summary>
        public const string CausationId = "CausationId";

        /// <summary>
        /// The caused by identifier key.
        /// </summary>
        public const string CausedBy = "CausedBy";

        /// <summary>
        /// The unique connection id from a client.
        /// </summary>
        public const string ConnectionId = "ConnectionId";
    }
}
