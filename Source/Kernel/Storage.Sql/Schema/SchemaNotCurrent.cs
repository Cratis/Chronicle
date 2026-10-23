// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Schema;

/// <summary>
/// The exception that is thrown when a versioned SQL schema is not current.
/// </summary>
/// <param name="database">The database name.</param>
/// <param name="scope">The schema scope.</param>
/// <param name="pending">The pending migrations or missing schema objects.</param>
public class SchemaNotCurrent(string database, string scope, IReadOnlyList<string> pending)
    : Exception($"The schema for '{scope}' in database '{database}' is not current ({string.Join(", ", pending)}). Run chronicle --migrate-only [--event-store X --namespace Y] before starting in Verify mode.")
{
    /// <summary>
    /// Gets the database name.
    /// </summary>
    public string Database { get; } = database;

    /// <summary>
    /// Gets the schema scope.
    /// </summary>
    public string Scope { get; } = scope;

    /// <summary>
    /// Gets pending migrations or missing objects.
    /// </summary>
    public IReadOnlyList<string> Pending { get; } = pending;
}
