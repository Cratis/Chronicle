// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Benchmark.Model;

public record PersonId(Guid Value) : ConceptAs<Guid>(Value)
{
    public static PersonId New() => new(Guid.NewGuid());
}
