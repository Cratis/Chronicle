// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.ExternalServices;

/// <summary>
/// Represents the type of endpoint an external service exposes.
/// </summary>
public enum ExternalServiceEndpointType
{
    /// <summary>
    /// An HTTP endpoint.
    /// </summary>
    Http = 0,

    /// <summary>
    /// A Microsoft SQL Server database endpoint.
    /// </summary>
    MsSql = 1,

    /// <summary>
    /// A PostgreSQL database endpoint.
    /// </summary>
    PostgreSql = 2
}
