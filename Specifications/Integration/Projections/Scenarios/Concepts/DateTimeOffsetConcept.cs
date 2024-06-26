// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Scenarios.Concepts;

public record DateTimeOffsetConcept(DateTimeOffset Value) : ConceptAs<DateTimeOffset>(Value)
{
    public static implicit operator DateTimeOffsetConcept(DateTimeOffset value) => new(value);
}
