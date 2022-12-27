// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;

namespace Aksio.Cratis.Configuration;

/// <summary>
/// Exception that gets thrown when configuration is missing for a specific configuration object type for a specific tenant.
/// </summary>
public class MissingConfigurationObjectForTenant : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MissingConfigurationObjectForTenant"/>.
    /// </summary>
    /// <param name="objectType">Type of configuration object.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="configurationFileName">The expected filename.</param>
    public MissingConfigurationObjectForTenant(
        Type objectType,
        TenantId tenantId,
        string configurationFileName)
        : base($"Missing configuration for object type '{objectType.FullName}' for tenant '{tenantId}'. Expecting a file with path and name '{configurationFileName}' in one of the search paths.")
    {
    }
}
