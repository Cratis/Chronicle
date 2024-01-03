// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Projections.Scenarios.Concepts;

public record FloatConcept(float Value) : ConceptAs<float>(Value)
{
    public static implicit operator FloatConcept(float value) => new(value);
}
