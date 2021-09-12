// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Configuration
{
    /// <summary>
    /// Defines a system that works with the configuration of <see cref="Storage"/>.
    /// </summary>
    public interface IStorageConfigurationManager
    {
        /// <summary>
        /// Get configuration for a tenant, serialized to the type asked for.
        /// </summary>
        /// <param name="targetType">Type to serialize to.</param>
        /// <param name="tenantId"><see cref="TenantId"/> to get for.</param>
        /// <returns>Instance of the configuration in the type asked for.</returns>
        object GetForEventStore(Type targetType, TenantId tenantId);
    }
}