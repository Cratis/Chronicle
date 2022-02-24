// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;

namespace Aksio.Cratis.Events.Store.MongoDB;

/// <summary>
/// Represents a key combination of <see cref="MicroserviceId"/> and <see cref="TenantId"/>.
/// </summary>
/// <param name="MicroserviceId">The <see cref="MicroserviceId"/>.</param>
/// <param name="TenantId">The <see cref="TenantId"/>.</param>
public record MicroserviceAndTenant(MicroserviceId MicroserviceId, TenantId TenantId);
