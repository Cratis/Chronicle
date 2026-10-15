// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_ScreenplayConformance.given;

/// <summary>
/// An event removing what a join correlated.
/// </summary>
/// <param name="Event">The event name.</param>
/// <param name="Key">The key of the joined instance.</param>
public record NeutralJoinRemoval(string Event, NeutralKey Key);
