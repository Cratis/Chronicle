// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Concepts.Customers;

public record CustomerId(Guid Value) : ConceptAs<Guid>(Value)
{
    public static implicit operator EventSourceId(CustomerId id) => new(id.ToString());
    public static implicit operator CustomerId(Guid value) => new(value);
    public static implicit operator CustomerId(string value) => new(Guid.Parse(value));
    public static implicit operator CustomerId(EventSourceId value) => new(Guid.Parse(value.Value));
}
