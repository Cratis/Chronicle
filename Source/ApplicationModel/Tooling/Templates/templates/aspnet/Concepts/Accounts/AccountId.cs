// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Concepts.Accounts;

public record AccountId(Guid Value) : ConceptAs<Guid>(Value)
{
    public static implicit operator AccountId(string value) => new(Guid.Parse(value));
    public static implicit operator EventSourceId(AccountId value) => new(value.ToString());
}
