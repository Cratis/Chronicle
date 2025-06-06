// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.InProcess.Integration.Projections.Concepts;

public record BoolConcept(bool Value) : ConceptAs<bool>(Value)
{
    public static implicit operator BoolConcept(bool value) => new(value);
}
