// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Grains.Events;

/// <summary>
/// Represents the event for when a domain specification has been set for an event store.
/// </summary>
/// <param name="EventStore">The event store name.</param>
/// <param name="Specification">The domain specification.</param>
[EventType]
public record EventStoreDomainSpecified(EventStoreName EventStore, DomainSpecification Specification);
