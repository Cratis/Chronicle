// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Concepts.AccountHolders;

public record AccountHolderId(Guid Value) : ConceptAs<Guid>(Value)
{
    public static implicit operator EventSourceId(AccountHolderId id) => new(id.ToString());
    public static implicit operator AccountHolderId(Guid value) => new(value);
    public static implicit operator AccountHolderId(string value) => new(Guid.Parse(value));
    public static implicit operator AccountHolderId(EventSourceId value) => new(Guid.Parse(value.Value));
}
