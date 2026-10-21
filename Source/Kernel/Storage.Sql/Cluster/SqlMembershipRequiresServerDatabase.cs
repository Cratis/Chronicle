// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Cluster;

/// <summary>
/// The exception that is thrown when SQL membership is configured without PostgreSQL or SQL Server.
/// </summary>
/// <param name="provider">The configured database provider.</param>
public sealed class SqlMembershipRequiresServerDatabase(string? provider)
    : Exception($"SQL membership requires PostgreSQL or SQL Server. Provider '{provider}' is not supported.");
