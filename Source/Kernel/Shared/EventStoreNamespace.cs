// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis;

/// <summary>
/// Represents the name and identifier of an event store.
/// </summary>
/// <param name="Value">The inner value.</param>
public record EventStoreNamespace(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="EventStoreNamespace"/>.
    /// </summary>
    /// <param name="value"><see cref="string"/> representation.</param>
    public static implicit operator EventStoreNamespace(string value) => new(value);

    /// <summary>
    /// Implicitly convert from <see cref="EventStoreNamespace"/> to <see cref="string"/>.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreNamespace"/> to convert from.</param>
    public static implicit operator string(EventStoreNamespace eventStore) => eventStore.Value;

    /// <summary>
    /// Implicitly convert from <see cref="TenantId"/> to <see cref="EventStoreNamespace"/>.
    /// </summary>
    /// <param name="tenantId"><see cref="TenantId"/> to convert from.</param>
    public static implicit operator EventStoreNamespace(TenantId tenantId) => new(tenantId.Value.ToString());
}
