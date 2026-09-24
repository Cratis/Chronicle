// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using EventType = Cratis.Chronicle.Contracts.Events.EventType;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents what a fluent projection declared about being one variant of a mutually exclusive group, carried
/// from the builder to the discovery pass that cross-wires the group.
/// </summary>
/// <param name="Identity">The type anchoring the logical identity the variants are grouped under.</param>
/// <param name="EnteringEventTypes">The event types that activate this variant.</param>
internal record FluentVariantDeclaration(Type Identity, IReadOnlyList<EventType> EnteringEventTypes);
