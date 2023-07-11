// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Tenants;

/// <summary>
/// Represents a tenant.
/// </summary>
/// <param name="Id">The unique identifier for the tenant.</param>
/// <param name="Name">Name of the tenant.</param>
public record Tenant(TenantId Id, string Name);
