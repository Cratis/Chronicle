// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Alerts;

/// <summary>
/// Scopes an incident read to an affected event store.
/// </summary>
/// <param name="EventStore">Required affected store.</param>
/// <param name="Namespace">Affected namespace, or all namespaces in the store.</param>
public record AlertIncidentScope(
    EventStoreName EventStore,
    EventStoreNamespaceName? Namespace);
