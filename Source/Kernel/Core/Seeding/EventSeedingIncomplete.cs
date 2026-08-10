// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Seeding;

/// <summary>
/// The exception that is thrown when at least one event offered for seeding was not appended.
/// </summary>
/// <param name="eventStore">The event store whose seeding is incomplete.</param>
/// <param name="eventStoreNamespace">The namespace whose seeding is incomplete, or <see cref="EventStoreNamespaceName.NotSet"/> for global seeding.</param>
public class EventSeedingIncomplete(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace)
    : Exception($"Event seeding for event store '{eventStore}' and namespace '{eventStoreNamespace}' is incomplete. Retry the same idempotent seed entries.");
