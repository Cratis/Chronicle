// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Kernel.Storage.MongoDB.Tenants;

/// <summary>
/// Represents the tenant configuration state stored in the database.
/// </summary>
/// <param name="Id">The tenant identifier.</param>
/// <param name="Configuration">Collection of <see cref="TenantConfigurationKeyValuePair"/>.</param>
public record TenantConfigurationState(TenantId Id, IEnumerable<TenantConfigurationKeyValuePair> Configuration);
