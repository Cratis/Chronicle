// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Concepts.given;

public record StringConcept(string Value) : ConceptAs<string>(Value)
{
    public static implicit operator StringConcept(string value) => new(value);
}
