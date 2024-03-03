// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Concepts;

namespace Cratis.Kernel.Projections.Scenarios.Concepts;

public record TimeOnlyConcept(TimeOnly Value) : ConceptAs<TimeOnly>(Value)
{
    public static implicit operator TimeOnlyConcept(TimeOnly value) => new(value);
}
