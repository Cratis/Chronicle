// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Kernel.Projections.Scenarios.Concepts;

public record IntConcept(int Value) : ConceptAs<int>(Value)
{
    public static implicit operator IntConcept(int value) => new(value);
}
