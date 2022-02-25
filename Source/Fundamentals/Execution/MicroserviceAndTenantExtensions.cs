// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Execution;

/// <summary>
/// Extension methods for working with <see cref="MicroserviceAndTenant"/>.
/// </summary>
public static class MicroserviceAndTenantExtensions
{
    /// <summary>
    /// Create <see cref="MicroserviceAndTenant"/> from information in <see cref="ExecutionContext"/>.
    /// </summary>
    /// <param name="context"><see cref="ExecutionContext"/> to create from.</param>
    /// <returns>A new <see cref="MicroserviceAndTenant"/> instance.</returns>
    public static MicroserviceAndTenant ToMicroserviceAndTenant(this ExecutionContext context) => new(context.MicroserviceId, context.TenantId);
}
