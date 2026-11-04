// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Naming;

namespace Cratis.Chronicle.EventStores;

/// <summary>
/// Represents the quarantined event stores and namespaces an operation that fans out left out.
/// </summary>
/// <param name="EventStores">The quarantined event stores that were left out.</param>
/// <param name="Namespaces">The quarantined namespaces of event stores that were not themselves quarantined, that were left out.</param>
public record SkippedResources(
    IReadOnlyList<EventStoreName> EventStores,
    IReadOnlyList<(EventStoreName EventStore, EventStoreNamespaceName Namespace)> Namespaces)
{
    /// <summary>
    /// Gets the <see cref="SkippedResources"/> with nothing left out.
    /// </summary>
    public static readonly SkippedResources None = new([], []);

    /// <summary>
    /// Gets whether any event store or namespace was left out.
    /// </summary>
    public bool Any => EventStores.Count > 0 || Namespaces.Count > 0;

    /// <summary>
    /// Gets the number of event stores and namespaces that were left out.
    /// </summary>
    public int Count => EventStores.Count + Namespaces.Count;

    /// <summary>
    /// Describes what was left out, with the names made safe for a log line.
    /// </summary>
    /// <returns>The description.</returns>
    public string Describe() => string.Join(
        ", ",
        EventStores.Select(_ => ResourceNameText.Escape(_.Value))
            .Concat(Namespaces.Select(_ => $"{ResourceNameText.Escape(_.EventStore.Value)}/{ResourceNameText.Escape(_.Namespace.Value)}")));
}
