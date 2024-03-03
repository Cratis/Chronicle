// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Configuration;

/// <summary>
/// Represents the configuration of a tenant.
/// </summary>
/// <param name="Id">The unique identifier of the tenant.</param>
/// <param name="Name">The name of the tenant.</param>
public record TenantInfo(TenantId Id, string Name);
