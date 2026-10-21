// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_ScreenplayConformance.given;

/// <summary>
/// An event removing an instance, a child or a nested object.
/// </summary>
/// <param name="Event">The event name.</param>
/// <param name="Key">The key identifying what is removed.</param>
/// <param name="ParentKey">The key identifying the parent, or null at a level without a parent.</param>
public record NeutralRemoval(string Event, NeutralKey Key, NeutralKey? ParentKey);
