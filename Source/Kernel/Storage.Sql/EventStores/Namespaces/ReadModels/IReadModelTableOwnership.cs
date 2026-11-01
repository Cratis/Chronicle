// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// Reads ownership metadata without opening, migrating or altering a dynamic model table.
/// Metadata of a neighbouring scope that cannot be read is reported as an unknown scope, never as an empty claimant set.
/// </summary>
internal interface IReadModelTableOwnership
{
    /// <summary>
    /// Reads all potentially competing owners sharing the resolved target and schema.
    /// </summary>
    /// <returns>The known claims and the scopes that could not be read.</returns>
    Task<ReadModelTableClaims> Read();

    /// <summary>
    /// Corroborates metadata ownership with expected key and column shape, without migration.
    /// </summary>
    /// <param name="physicalName">The exact catalog table name.</param>
    /// <returns>Whether the existing table has the expected read-model shape.</returns>
    Task<bool> HasExpectedShape(string physicalName);
}
