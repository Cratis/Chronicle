// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events;
using Cratis.Concepts;

namespace Basic;

public record Price(decimal Value) : ConceptAs<decimal>(Value)
{
    public static implicit operator Price(decimal value) => new(value);
}

public record Description(string Value) : ConceptAs<string>(Value)
{
    public static implicit operator Description(string value) => new(value);
}

[EventType("147077c9-3954-4931-9a29-ea750bff97c1")]
public record ItemAddedToCart(PersonId PersonId, MaterialId MaterialId, int Quantity, Price? Price, Description? Description);
