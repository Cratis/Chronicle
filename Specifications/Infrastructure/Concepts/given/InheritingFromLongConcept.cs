// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Concepts.given;

public record InheritingFromLongConcept(long Value) : LongConcept(Value)
{
    public static implicit operator InheritingFromLongConcept(long value) => new(value);
}
