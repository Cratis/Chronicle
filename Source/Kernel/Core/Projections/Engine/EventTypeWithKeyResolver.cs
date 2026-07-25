// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// Represents a combination of <see cref="EventType"/> and <see cref="KeyResolver"/>.
/// </summary>
/// <param name="EventType">The <see cref="EventType"/>.</param>
/// <param name="KeyResolver">The <see cref="KeyResolver"/> for resolving the key.</param>
/// <param name="ResolvesToEventSourceId">
/// Whether the <see cref="KeyResolver"/> resolves the read-model key directly to the event's own event source id
/// (the <c>FromEventSourceId</c> resolver). When <see langword="true"/> for every event type a projection handles,
/// events for different event source ids always target different read-model documents, so the pipeline may
/// serialize handling striped per event source id rather than across the whole projection. Any resolver that can
/// collapse distinct event sources onto one document (constant key, join, parent hierarchy, or a value read from
/// the event content) leaves this <see langword="false"/>.
/// </param>
public record EventTypeWithKeyResolver(EventType EventType, KeyResolver KeyResolver, bool ResolvesToEventSourceId = false);
