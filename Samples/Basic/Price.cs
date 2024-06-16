// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Concepts;

namespace Basic;

public record Price(decimal Value) : ConceptAs<decimal>(Value)
{
    public static implicit operator Price(decimal value) => new(value);
}
