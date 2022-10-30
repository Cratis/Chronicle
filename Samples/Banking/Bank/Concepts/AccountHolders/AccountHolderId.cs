// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Concepts.AccountHolders;

public record AccountHolderId(string Value) : ConceptAs<string>(Value)
{
    public static implicit operator EventSourceId(AccountHolderId id) => new(id.ToString());
    public static implicit operator AccountHolderId(EventSourceId id) => new(id.Value);
    public static implicit operator AccountHolderId(string value) => new(value);
}
