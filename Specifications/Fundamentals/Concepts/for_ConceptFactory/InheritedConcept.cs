﻿// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Concepts.for_ConceptFactory
{
    public record InheritedConcept(long Value) : LongConcept(Value);
}
