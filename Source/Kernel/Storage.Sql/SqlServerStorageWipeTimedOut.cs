// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEVELOPMENT
using Microsoft.Data.SqlClient;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// The exception that is thrown when a SQL Server storage wipe times out.
/// </summary>
/// <param name="databaseName">The name of the database being wiped.</param>
/// <param name="sessionId">The SQL Server session performing the wipe.</param>
/// <param name="diagnostics">The request, session and blocker diagnostics.</param>
/// <param name="innerException">The original SQL Server timeout.</param>
public sealed class SqlServerStorageWipeTimedOut(string databaseName, int sessionId, string diagnostics, SqlException innerException)
    : Exception($"SQL Server storage wipe timed out in database '{databaseName}' (session {sessionId}). {diagnostics}", innerException);
#endif
