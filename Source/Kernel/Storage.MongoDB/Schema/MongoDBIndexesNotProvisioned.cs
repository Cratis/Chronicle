// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Schema;

/// <summary>
/// Exception representing MongoDB scopes whose indexes have not been provisioned.
/// </summary>
/// <param name="report">The index report.</param>
public class MongoDBIndexesNotProvisioned(MongoDBIndexReport report) : Exception(
    $"MongoDB indexes are not provisioned: {string.Join("; ", report.Scopes.Where(scope => scope.Missing.Count + scope.Obsolete.Count > 0).Select(scope => $"{scope.Scope} ({scope.Database}): missing [{string.Join(", ", scope.Missing)}], obsolete [{string.Join(", ", scope.Obsolete)}]"))}");
