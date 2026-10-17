// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// The exception that is thrown when a database does not exist and Chronicle is configured not to create it.
/// </summary>
/// <param name="database">The name of the database.</param>
public class DatabaseDoesNotExist(string database)
    : Exception($"The database '{database}' does not exist, and storage is configured not to create databases ('CreateIfMissing' is false). Provision the database before Chronicle uses it.");
