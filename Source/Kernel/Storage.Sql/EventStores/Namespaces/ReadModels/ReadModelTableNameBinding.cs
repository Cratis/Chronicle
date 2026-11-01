// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// An immutable physical ownership assignment with durable provision and replay state.
/// </summary>
internal sealed class ReadModelTableNameBinding
{
    /// <summary>Gets or sets the digest lookup key.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the full originating event store.</summary>
    public string EventStoreId { get; set; } = string.Empty;

    /// <summary>Gets or sets the full originating namespace.</summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>Gets or sets the registered model identifier.</summary>
    public string ReadModelIdentifier { get; set; } = string.Empty;

    /// <summary>Gets or sets the original full container name.</summary>
    public string ContainerName { get; set; } = string.Empty;

    /// <summary>Gets or sets the logical table role.</summary>
    public ReadModelTableRole Role { get; set; }

    /// <summary>Gets or sets the full logical table name.</summary>
    public string LogicalName { get; set; } = string.Empty;

    /// <summary>Gets or sets the permanent bounded physical identifier.</summary>
    public string PhysicalName { get; set; } = string.Empty;

    /// <summary>Gets or sets the durable provision state.</summary>
    public ReadModelTableBindingState State { get; set; }

    /// <summary>Gets or sets the replay shadow's full logical attempt, never cached.</summary>
    public string? ReplayAttempt { get; set; }

    /// <summary>
    /// Gets every logical field, not just the digest or physical placement.
    /// </summary>
    internal ReadModelTableOwner Owner => new(EventStoreId, Namespace, ReadModelIdentifier, ContainerName, Role, LogicalName);

    /// <summary>
    /// Creates a reservation or adoption without modifying user tables.
    /// </summary>
    /// <param name="owner">The complete owner.</param>
    /// <param name="physicalName">The exact catalog name or new bounded candidate.</param>
    /// <param name="state">The initial state.</param>
    /// <returns>The binding.</returns>
    internal static ReadModelTableNameBinding For(ReadModelTableOwner owner, string physicalName, ReadModelTableBindingState state) => new()
    {
        Id = owner.Id,
        EventStoreId = owner.EventStoreId,
        Namespace = owner.Namespace,
        ReadModelIdentifier = owner.ReadModelIdentifier,
        ContainerName = owner.ContainerName,
        Role = owner.Role,
        LogicalName = owner.LogicalName,
        PhysicalName = physicalName,
        State = state
    };
}
