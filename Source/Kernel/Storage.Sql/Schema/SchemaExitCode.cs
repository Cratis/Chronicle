// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Cluster;

namespace Cratis.Chronicle.Storage.Sql.Schema;

/// <summary>
/// Maps SQL schema operation failures to the server command's exit codes.
/// </summary>
public static class SchemaExitCode
{
    /// <summary>
    /// Gets the exit code for a failed schema action.
    /// </summary>
    /// <param name="exception">The failure.</param>
    /// <param name="verify">Whether the action was verification.</param>
    /// <returns>The documented schema-action exit code.</returns>
    public static int For(Exception exception, bool verify) => exception switch
    {
        SchemaNotCurrent => 2,
        DatabaseSchemaLockNotAcquired => 3,
        DatabaseDoesNotExist or SchemaDoesNotExist => verify ? 2 : 4,
        DatabaseNameBindingFailure or DefaultSchemaMismatch or ArgumentException or InvalidOperationException => 4,
        _ => 1
    };
}
