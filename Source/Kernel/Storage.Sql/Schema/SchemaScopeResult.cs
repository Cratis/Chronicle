// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Schema;

/// <summary>
/// The migration history and missing objects for one schema scope.
/// </summary>
/// <param name="Scope">The logical scope.</param>
/// <param name="Database">The physical database name.</param>
/// <param name="Pending">Pending migration identifiers or missing objects.</param>
/// <param name="Unknown">Applied migrations not known to this context.</param>
public record SchemaScopeResult(string Scope, string Database, IReadOnlyList<string> Pending, IReadOnlyList<string> Unknown);
