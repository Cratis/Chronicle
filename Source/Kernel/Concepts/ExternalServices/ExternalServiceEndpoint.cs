// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.ExternalServices;

/// <summary>
/// Represents the endpoint an external service exposes.
/// </summary>
/// <param name="Type">The <see cref="ExternalServiceEndpointType"/> discriminating which configuration applies.</param>
/// <param name="Http">The HTTP configuration, set when <see cref="Type"/> is <see cref="ExternalServiceEndpointType.Http"/>.</param>
/// <param name="Database">The database configuration, set for database endpoint types (MSSQL, PostgreSQL, ...).</param>
/// <remarks>
/// A single flat record keeps the endpoint trivially serializable across storage, contracts and the
/// Workbench while the <see cref="Type"/> discriminator keeps the model open to new endpoint types.
/// </remarks>
public record ExternalServiceEndpoint(
    ExternalServiceEndpointType Type,
    HttpEndpointConfiguration? Http = default,
    DatabaseEndpointConfiguration? Database = default)
{
    /// <summary>
    /// Gets a value indicating whether the endpoint is a database endpoint.
    /// </summary>
    public bool IsDatabase => Type is ExternalServiceEndpointType.MsSql or ExternalServiceEndpointType.PostgreSql;
}
