// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// The exception that is thrown when the database user Chronicle connects as does not have the configured schema as its default schema.
/// </summary>
/// <param name="database">The name of the database.</param>
/// <param name="schema">The configured schema.</param>
/// <param name="actual">The default schema of the database user.</param>
public class DefaultSchemaMismatch(string database, string schema, string actual)
    : Exception($"Chronicle is configured to keep its tables in the schema '{schema}', but the database user connecting to '{database}' has '{actual}' as its default schema. SQL Server places unqualified tables in the default schema of the user, so set it, for example with ALTER USER [user] WITH DEFAULT_SCHEMA = [{schema}].");
