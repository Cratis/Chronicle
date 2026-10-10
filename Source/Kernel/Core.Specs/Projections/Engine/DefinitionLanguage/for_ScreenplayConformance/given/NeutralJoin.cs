// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_ScreenplayConformance.given;

/// <summary>
/// One joined event.
/// </summary>
/// <param name="Event">The event name.</param>
/// <param name="On">The read-model property the join correlates on.</param>
/// <param name="Key">The join key, or null when the join has none.</param>
/// <param name="Mappings">The effective mappings, AutoMap included.</param>
public record NeutralJoin(string Event, string On, NeutralKey? Key, IReadOnlyList<NeutralMapping> Mappings);
