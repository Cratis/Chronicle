// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts;

/// <summary>
/// Represents the identifier of an event store namespace.
/// </summary>
/// <param name="Value">The inner value.</param>
public record EventStoreNamespaceId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>
    /// Gets the Default <see cref="EventStoreNamespaceId"/>.
    /// </summary>
    public static readonly EventStoreNamespaceId Default = Guid.Empty;

    /// <summary>
    /// Implicitly convert from <see cref="Guid"/> to <see cref="EventStoreNamespaceId"/>.
    /// </summary>
    /// <param name="value"><see cref="Guid"/> representation.</param>
    public static implicit operator EventStoreNamespaceId(Guid value) => new(value);

    /// <summary>
    /// Implicitly convert from <see cref="EventStoreNamespaceId"/> to <see cref="Guid"/>.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreNamespaceId"/> to convert from.</param>
    public static implicit operator Guid(EventStoreNamespaceId eventStore) => eventStore.Value;
}
