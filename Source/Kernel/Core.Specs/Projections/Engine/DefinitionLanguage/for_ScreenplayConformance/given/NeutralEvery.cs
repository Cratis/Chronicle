// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_ScreenplayConformance.given;

/// <summary>
/// The <c language="csharp">every</c> or <c language="csharp">all</c> mappings of a level.
/// </summary>
/// <param name="IncludeChildren">Whether the mappings also run for the events of child levels.</param>
/// <param name="SubscribesToAllEvents">Whether the level subscribes to every event type.</param>
/// <param name="Mappings">The mappings.</param>
public record NeutralEvery(bool IncludeChildren, bool SubscribesToAllEvents, IReadOnlyList<NeutralMapping> Mappings);
