// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Imports;

/// <summary>
/// Represents an import session row.
/// </summary>
public class ImportEntity
{
    /// <summary>
    /// Gets or sets the import identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the import state.
    /// </summary>
    public int State { get; set; }

    /// <summary>
    /// Gets or sets the serialized import record.
    /// </summary>
    public string Payload { get; set; } = string.Empty;
}
