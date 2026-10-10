// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_ScreenplayConformance.given;

/// <summary>
/// One transition: an event that creates or updates an instance at its level.
/// </summary>
/// <param name="Event">The event name.</param>
/// <param name="Key">The key identifying the instance.</param>
/// <param name="ParentKey">The key identifying the parent instance, or null at a level without a parent.</param>
/// <param name="Mappings">The effective mappings, AutoMap included.</param>
public record NeutralFrom(string Event, NeutralKey Key, NeutralKey? ParentKey, IReadOnlyList<NeutralMapping> Mappings);
