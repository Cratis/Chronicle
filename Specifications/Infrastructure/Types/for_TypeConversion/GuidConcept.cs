// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Types.for_TypeConversion;

public record GuidConcept(Guid Value) : ConceptAs<Guid>(Value)
{
    public static implicit operator GuidConcept(Guid value) => new(value);
}
