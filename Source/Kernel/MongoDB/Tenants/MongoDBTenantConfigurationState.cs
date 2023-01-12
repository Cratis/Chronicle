// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;

namespace Aksio.Cratis.Kernel.MongoDB.Tenants;

/// <summary>
/// Represents the tenant configuration state stored in the database.
/// </summary>
/// <param name="Id">The tenant identifier.</param>
/// <param name="Configuration">Collection of <see cref="MongoDBTenantConfigurationKeyValuePair"/>.</param>
public record MongoDBTenantConfigurationState(TenantId Id, IEnumerable<MongoDBTenantConfigurationKeyValuePair> Configuration);
