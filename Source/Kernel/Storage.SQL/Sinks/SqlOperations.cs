// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.SQL.Sinks;

/// <summary>
/// Represents SQL operations to be executed.
/// </summary>
/// <param name="Operations">The collection of SQL operations.</param>
public record SqlOperations(IEnumerable<SqlOperation> Operations)
{
    /// <summary>
    /// Gets a value indicating whether there are any changes to apply.
    /// </summary>
    public bool HasChanges => Operations.Any();

    /// <summary>
    /// Gets an empty SQL operations instance.
    /// </summary>
    public static readonly SqlOperations Empty = new([]);
}