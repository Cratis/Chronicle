// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Naming;

/// <summary>
/// Describes a new event store or namespace for <see cref="IResourceNameGate.EnsureCanCreate"/>.
/// </summary>
/// <param name="EventStore">The event store name.</param>
/// <param name="Namespace">The namespace name, or null when creating the event store.</param>
/// <param name="RegisteredNames">The names that already exist: the event stores, or the namespaces of the event store.</param>
/// <param name="RegisteredEventStores">When creating a namespace, all existing event stores.</param>
/// <param name="NamespacesOf">When creating a namespace, reads the namespaces of another event store.</param>
public sealed record ResourceCreation(
    EventStoreName EventStore,
    EventStoreNamespaceName? Namespace,
    IEnumerable<string> RegisteredNames,
    IEnumerable<string>? RegisteredEventStores = null,
    Func<string, Task<IEnumerable<string>>>? NamespacesOf = null);
