// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Naming;

/// <summary>
/// Represents the owner of a resource: an event store, or one of its namespaces.
/// </summary>
/// <param name="EventStore">The event store name.</param>
/// <param name="Namespace">The namespace name, or null when the owner is the event store itself.</param>
public record ResourceOwner(string EventStore, string? Namespace = null);
