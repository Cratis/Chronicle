// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Concepts;

namespace Aksio.Cratis.Kernel.Projections.Scenarios.Concepts;

public record DateTimeOffsetConcept(DateTimeOffset Value) : ConceptAs<DateTimeOffset>(Value)
{
    public static implicit operator DateTimeOffsetConcept(DateTimeOffset value) => new(value);
}
