// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// Refuses a new reservation, adoption or table creation while a possible owner's metadata cannot be read.
/// </summary>
public sealed class ReadModelTableMetadataUnavailable : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReadModelTableMetadataUnavailable"/> class.
    /// </summary>
    /// <param name="owner">The requested owner.</param>
    /// <param name="unknown">The scopes that could not be read.</param>
    internal ReadModelTableMetadataUnavailable(ReadModelTableOwner owner, IEnumerable<ReadModelTableMetadataScope> unknown)
        : base($"Cannot bind read model table {owner}: metadata of possible owners could not be read ({string.Join("; ", unknown.Select(scope => scope.Describe()))}). Grant catalog visibility or bind the scopes; existing Ready tables keep serving.")
    {
    }
}
