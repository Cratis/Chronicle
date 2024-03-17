// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Concepts.given;

public record MultiLevelInheritanceConcept(long Value) : InheritingFromLongConcept(Value)
{
    public static implicit operator MultiLevelInheritanceConcept(long value) => new(value);
}
