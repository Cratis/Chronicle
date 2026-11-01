// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// Refuses an unproved or conflicting ownership decision before touching user data.
/// </summary>
public sealed class ReadModelTableNameBindingFailure : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReadModelTableNameBindingFailure"/> class.
    /// </summary>
    /// <param name="owner">The complete requested owner.</param>
    /// <param name="candidate">The physical candidate being considered.</param>
    /// <param name="reason">The recovery or conflict reason, without connection details.</param>
    /// <param name="conflict">The conflicting owner, if known.</param>
    internal ReadModelTableNameBindingFailure(ReadModelTableOwner owner, string candidate, string reason, ReadModelTableOwner? conflict = null)
        : base($"Cannot bind read model table {owner} to '{candidate}': {reason}. Conflicting owner: {conflict?.ToString() ?? "unknown"}.")
    {
    }
}
