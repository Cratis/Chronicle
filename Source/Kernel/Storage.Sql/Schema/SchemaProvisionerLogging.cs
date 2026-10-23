// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.Schema;

/// <summary>
/// Structured diagnostics for SQL schema operations.
/// </summary>
public static partial class SchemaProvisionerLogging
{
    [LoggerMessage(LogLevel.Information, "Schema action {Action} starting")]
    public static partial void SchemaActionStarting(ILogger logger, string action);

    [LoggerMessage(LogLevel.Information, "Schema action {Action} completed with exit code {ExitCode} after {Elapsed}")]
    public static partial void SchemaActionCompleted(ILogger logger, string action, int exitCode, TimeSpan elapsed);

    [LoggerMessage(LogLevel.Information, "No versioned schema for storage type {StorageType}")]
    public static partial void NoVersionedSchema(ILogger logger, string storageType);

    [LoggerMessage(LogLevel.Warning, "SchemaMode=Verify is ignored for storage type {StorageType}")]
    public static partial void SchemaModeIgnored(ILogger logger, string storageType);

    [LoggerMessage(LogLevel.Error, "Schema action failed")]
    public static partial void SchemaActionFailed(ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Information, "Schema scope {Scope} in database {Database} is current after {Elapsed}")]
    internal static partial void ScopeCurrent(ILogger logger, string scope, string database, TimeSpan elapsed);

    [LoggerMessage(LogLevel.Warning, "Schema scope {Scope} in database {Database} is pending: {Pending}")]
    internal static partial void ScopePending(ILogger logger, string scope, string database, string pending);

    [LoggerMessage(LogLevel.Information, "Database {Database} has migrations unknown to this context: {Unknown}")]
    internal static partial void NewerMigrationsPresent(ILogger logger, string database, string unknown);
}
