// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration;

/// <summary>
/// Represents the way a projection is expressed in client code.
/// </summary>
public enum ProjectionCodeStyle
{
    /// <summary>
    /// The projection is defined separately from the read model it targets.
    /// </summary>
    Declarative = 0,

    /// <summary>
    /// The projection is expressed on the read model itself.
    /// </summary>
    ModelBound = 1
}
