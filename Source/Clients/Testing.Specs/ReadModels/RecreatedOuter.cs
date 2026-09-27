// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

/// <summary>The outer nested level.</summary>
public record RecreatedOuter
{
    /// <summary>Gets or sets the inner object.</summary>
    public RecreatedInfo? Info { get; set; }
}
