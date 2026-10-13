// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_children_with_nested_identities;

/// <summary>
/// The children briefed for an event source.
/// </summary>
/// <param name="Children">The briefed children.</param>
public record NestedIdentityReadModel(IEnumerable<NestedIdentityChild> Children);
