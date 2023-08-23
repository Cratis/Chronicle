// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Orleans.Execution;

/// <summary>
/// Holds all the keys used in the Orleans Request Context.
/// </summary>
/// <remarks>
/// See <see cref="ExecutionContextOutgoingCallFilter"/> and <see cref="ExecutionContextIncomingCallFilter"/> for usage.
/// </remarks>
public static class RequestContextKeys
{
    /// <summary>
    /// The microservice identifier key.
    /// </summary>
    public const string MicroserviceId = "MicroserviceId";

    /// <summary>
    /// The tenant identifier key.
    /// </summary>
    public const string TenantId = "TenantId";

    /// <summary>
    /// The correlation identifier key.
    /// </summary>
    public const string CorrelationId = "CorrelationId";
}
