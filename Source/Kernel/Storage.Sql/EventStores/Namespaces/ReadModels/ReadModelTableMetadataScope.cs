// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// Borrowed, unmigrated metadata contexts for a logical scope which may share the resolved placement.
/// The caller enumerates scopes from configured cluster metadata and retains their original identity.
/// An unknown scope has no contexts: its metadata could not be located or read, so it is a possible owner.
/// </summary>
/// <param name="EventStoreId">The complete originating event store, or "*" when the registry itself was unreadable.</param>
/// <param name="Namespace">The complete originating namespace, or null for any namespace of the store.</param>
/// <param name="Definitions">Existing event-store metadata, not a dynamic model context.</param>
/// <param name="History">Existing namespace metadata, not a dynamic model context.</param>
/// <param name="Stage">The unknown stage, when this scope is unknown.</param>
/// <param name="Reason">A short fixed reason or exception type name, never a message or connection string.</param>
internal sealed record ReadModelTableMetadataScope(
    string EventStoreId,
    string? Namespace,
    EventStoreDbContext? Definitions,
    NamespaceDbContext? History,
    string? Stage = null,
    string? Reason = null) : IAsyncDisposable
{
    /// <summary>
    /// Gets a value indicating whether this scope could not be located or read.
    /// </summary>
    internal bool IsUnknown => Stage is not null;

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (Definitions is not null)
        {
            await Definitions.DisposeAsync();
        }

        if (History is not null)
        {
            await History.DisposeAsync();
        }
    }

    /// <summary>
    /// Creates a scope whose metadata is unknown and so counts as a possible owner.
    /// </summary>
    /// <param name="eventStoreId">The event store, or "*" for the registry.</param>
    /// <param name="namespaceName">The namespace, or null for any namespace of the store.</param>
    /// <param name="stage">The stage that could not be answered.</param>
    /// <param name="reason">A short fixed reason or exception type name.</param>
    /// <returns>The unknown scope.</returns>
    internal static ReadModelTableMetadataScope Unknown(string eventStoreId, string? namespaceName, string stage, string reason) =>
        new(eventStoreId, namespaceName, null, null, stage, reason);

    /// <summary>
    /// Describes the unknown scope without connection details.
    /// </summary>
    /// <returns>The description.</returns>
    internal string Describe() => $"{EventStoreId}/{Namespace ?? "*"} ({Stage}: {Reason})";
}
