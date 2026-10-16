// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Namespaces;

/// <summary>
/// Exception that gets thrown when removing a namespace that must always exist.
/// </summary>
/// <param name="eventStore">The <see cref="EventStoreName"/> of the event store.</param>
/// <param name="namespace">The <see cref="EventStoreNamespaceName"/> that could not be removed.</param>
public class NamespaceCannotBeRemoved(EventStoreName eventStore, EventStoreNamespaceName @namespace)
    : Exception($"Namespace '{@namespace}' in event store '{eventStore}' is the default namespace and cannot be removed");
