// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.InProcess.Integration.Projections.Concepts;

public record GuidConcept(Guid Value) : ConceptAs<Guid>(Value)
{
    public static implicit operator GuidConcept(Guid value) => new(value);
}
