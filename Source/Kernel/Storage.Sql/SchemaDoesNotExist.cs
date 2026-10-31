// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// The exception that is thrown when a schema does not exist and Chronicle is configured not to create it.
/// </summary>
/// <param name="database">The name of the database.</param>
/// <param name="schema">The name of the schema.</param>
public class SchemaDoesNotExist(string database, string schema)
    : Exception($"The schema '{schema}' does not exist in the database '{database}', and storage is configured not to create schemas ('CreateIfMissing' is false). Provision the schema before Chronicle uses it.");
