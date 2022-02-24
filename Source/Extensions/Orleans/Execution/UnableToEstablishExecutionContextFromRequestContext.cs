// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Extensions.Orleans.Execution;

/// <summary>
/// Exception that gets thrown when it is not possible to establish execution context from the Orleans request context.
/// </summary>
public class UnableToEstablishExecutionContextFromRequestContext : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnableToEstablishExecutionContextFromRequestContext"/> class.
    /// </summary>
    public UnableToEstablishExecutionContextFromRequestContext() : base("Unable to establish Execution Context, missing required keys (MicroserviceId & TenantId) from the Orleans Request Context")
    {
    }
}
