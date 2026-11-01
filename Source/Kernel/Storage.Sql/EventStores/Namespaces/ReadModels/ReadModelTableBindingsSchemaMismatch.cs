// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// A relation occupying the ledger name is not the expected ownership metadata schema.
/// </summary>
public sealed class ReadModelTableBindingsSchemaMismatch : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReadModelTableBindingsSchemaMismatch"/> class.
    /// </summary>
    internal ReadModelTableBindingsSchemaMismatch()
        : base("The read model table-name ledger has an incompatible shape; recovery required.")
    {
    }
}
