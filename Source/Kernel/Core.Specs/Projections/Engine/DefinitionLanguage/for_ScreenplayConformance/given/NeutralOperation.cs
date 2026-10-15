// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_ScreenplayConformance.given;

/// <summary>
/// The operation a neutral mapping performs on its target.
/// </summary>
public enum NeutralOperation
{
    Set = 0,
    Clear = 1,
    Add = 2,
    Subtract = 3,
    Increment = 4,
    Decrement = 5
}
