// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Concepts;

namespace Basic;

public record Description(string Value) : ConceptAs<string>(Value)
{
    public static implicit operator Description(string value) => new(value);
}
