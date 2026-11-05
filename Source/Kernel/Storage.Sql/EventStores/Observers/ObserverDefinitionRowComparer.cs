// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Observers;

/// <summary>
/// Compares persisted observer definition rows without mapping them to kernel definitions.
/// </summary>
public sealed class ObserverDefinitionRowComparer : IEqualityComparer<ObserverDefinition>, IEqualityComparer
{
    /// <summary>
    /// The shared comparer instance.
    /// </summary>
    public static readonly ObserverDefinitionRowComparer Instance = new();

    /// <inheritdoc/>
    public bool Equals(ObserverDefinition? x, ObserverDefinition? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;
        return x.Id == y.Id &&
            x.EventTypes.SequenceEqual(y.EventTypes) &&
            x.EventSequenceId == y.EventSequenceId &&
            x.Type == y.Type &&
            x.Owner == y.Owner &&
            x.IsReplayable == y.IsReplayable &&
            x.GenerationDelivery == y.GenerationDelivery;
    }

    /// <inheritdoc/>
    public int GetHashCode(ObserverDefinition obj) => obj.Id.GetHashCode();

    /// <inheritdoc/>
    bool IEqualityComparer.Equals(object? x, object? y) => Equals(x as ObserverDefinition, y as ObserverDefinition);

    /// <inheritdoc/>
    int IEqualityComparer.GetHashCode(object obj) => GetHashCode((ObserverDefinition)obj);
}
