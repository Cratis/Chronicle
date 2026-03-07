// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Namespaces;

/// <summary>
/// Represents the message for a namespace that has been added.
/// </summary>
/// <param name="EventStore">Event store that the namespace was added to.</param>
/// <param name="Namespace">Namespace that was added.</param>
[EventType]
public record NamespaceAdded(EventStoreName EventStore, EventStoreNamespaceName Namespace);
