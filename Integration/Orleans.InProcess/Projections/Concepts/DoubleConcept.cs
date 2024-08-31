// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Concepts;

public record DoubleConcept(double Value) : ConceptAs<double>(Value)
{
    public static implicit operator DoubleConcept(double value) => new(value);
}
