// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Chronicle.Projections.Scenarios.Concepts;

public record DateTimeConcept(DateTime Value) : ConceptAs<DateTime>(Value)
{
    public static implicit operator DateTimeConcept(DateTime value) => new(value);
}
