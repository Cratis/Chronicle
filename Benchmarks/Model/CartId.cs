// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Concepts;
using Cratis.Chronicle.Events;

namespace Benchmark.Model;

public record CartId(Guid Value) : ConceptAs<Guid>(Value)
{
    public static implicit operator CartId(Guid value) => new(value);

    public static implicit operator CartId(EventSourceId value) => new(Guid.Parse(value.Value));
}
