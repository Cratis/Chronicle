// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// Borrowed, unmigrated metadata contexts for a logical scope which may share the resolved placement.
/// The caller enumerates scopes from configured cluster metadata and retains their original identity.
/// </summary>
/// <param name="EventStoreId">The complete originating event store.</param>
/// <param name="Namespace">The complete originating namespace.</param>
/// <param name="Definitions">Existing event-store metadata, not a dynamic model context.</param>
/// <param name="History">Existing namespace metadata, not a dynamic model context.</param>
internal sealed record ReadModelTableMetadataScope(string EventStoreId, string Namespace, EventStoreDbContext Definitions, NamespaceDbContext History);
