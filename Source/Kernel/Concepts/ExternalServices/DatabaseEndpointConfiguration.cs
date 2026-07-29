// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Concepts.ExternalServices;

/// <summary>
/// Represents the configuration common to database external service endpoints (MSSQL, PostgreSQL, ...).
/// </summary>
/// <param name="Host">The database host.</param>
/// <param name="Port">The database port. Use <see cref="DatabasePort.Unspecified"/> to use the provider default.</param>
/// <param name="Database">The database name.</param>
/// <param name="Username">The username used to connect.</param>
/// <param name="Password">The password used to connect.</param>
/// <param name="Options">Additional provider-specific options appended to the connection string.</param>
public record DatabaseEndpointConfiguration(
    DatabaseHost Host,
    DatabasePort Port,
    DatabaseName Database,
    Username Username,
    Password Password,
    IReadOnlyDictionary<string, string> Options);
