// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis;

/// <summary>
/// Represents the name and identifier of an event store.
/// </summary>
/// <param name="Value">The inner value.</param>
public record EventStoreNamespaceName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets the default <see cref="EventStoreNamespaceName"/>.
    /// </summary>
    public static readonly EventStoreNamespaceName Default = "Default";

    /// <summary>
    /// Gets the not set <see cref="EventStoreNamespaceName"/>.
    /// </summary>
    public static readonly EventStoreNamespaceName NotSet = "[NotSet]";

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="EventStoreNamespaceName"/>.
    /// </summary>
    /// <param name="value"><see cref="string"/> representation.</param>
    public static implicit operator EventStoreNamespaceName(string value) => new(value);

    /// <summary>
    /// Implicitly convert from <see cref="EventStoreNamespaceName"/> to <see cref="string"/>.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreNamespaceName"/> to convert from.</param>
    public static implicit operator string(EventStoreNamespaceName eventStore) => eventStore.Value;
}
