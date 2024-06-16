// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Scenarios.Concepts;

public record EnumWithValuesConcept(EnumWithValues Value) : ConceptAs<EnumWithValues>(Value)
{
    public static implicit operator EnumWithValuesConcept(EnumWithValues value) => new(value);
}
