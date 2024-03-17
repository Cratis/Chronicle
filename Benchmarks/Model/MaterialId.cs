// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Benchmark.Model;

public record MaterialId(Guid Value) : ConceptAs<Guid>(Value)
{
    public static MaterialId New() => new(Guid.NewGuid());
}
