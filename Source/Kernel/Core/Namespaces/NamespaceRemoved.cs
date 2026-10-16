// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Namespaces;

/// <summary>
/// Represents the fact that a namespace has been removed, together with its observers and stored data.
/// </summary>
/// <param name="EventStore">Event store that the namespace was removed from.</param>
/// <param name="Namespace">Namespace that was removed.</param>
[EventType]
public record NamespaceRemoved(EventStoreName EventStore, EventStoreNamespaceName Namespace);
