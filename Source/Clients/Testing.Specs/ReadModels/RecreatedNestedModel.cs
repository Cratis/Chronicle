// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

/// <summary>A read model with two nested levels.</summary>
public record RecreatedNestedModel
{
    /// <summary>Gets or sets the outer object.</summary>
    public RecreatedOuter? Outer { get; set; }
}
