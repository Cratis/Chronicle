// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

/// <summary>A stable generation capability wire name.</summary>
/// <param name="Value">A nonempty name without comma or unit separator.</param>
public record GenerationCapability(string Value) : ConceptAs<string>(!string.IsNullOrWhiteSpace(Value) && !Value.Contains(',') && !Value.Contains('\u001f') ? Value : throw new ArgumentException("Invalid generation capability.", nameof(Value)))
{
    /// <summary>Pre-mutation floor admission.</summary>
    public static readonly GenerationCapability FloorAdmission = new("FloorAdmission");

    /// <summary>In-place legacy materialization adoption.</summary>
    public static readonly GenerationCapability InPlaceAdoption = new("InPlaceAdoption");

    /// <summary>Binding epoch fencing.</summary>
    public static readonly GenerationCapability BindingEpoch = new("BindingEpoch");
}
