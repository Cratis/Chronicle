// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_ScreenplayConformance.given;

/// <summary>
/// The kinds of value a neutral mapping or key reads.
/// </summary>
public enum NeutralValueKind
{
    EventProperty = 0,
    EventSourceId = 1,
    EventContext = 2,
    Text = 3,
    Number = 4,
    Boolean = 5,
    Unsupported = 6
}
