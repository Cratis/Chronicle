// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents native Microsoft Entra authentication for Azure Database for PostgreSQL.
/// </summary>
public class PostgreSqlEntra
{
    /// <summary>
    /// Gets a value indicating whether PostgreSQL passwords are obtained from Microsoft Entra.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// Gets the optional client id of a user-assigned managed identity for DefaultAzureCredential.
    /// </summary>
    public string? ManagedIdentityClientId { get; init; }
}
