// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage;

/// <summary>The projection level that owns a declaration block.</summary>
internal enum ProjectionLevel
{
    /// <summary>The root read model.</summary>
    Root = 0,

    /// <summary>A child collection.</summary>
    Children = 1,

    /// <summary>A scalar nested object.</summary>
    Nested = 2,

    /// <summary>A scalar nested object inside a child collection, including deeper nested objects.</summary>
    NestedInChildren = 3
}
