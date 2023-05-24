// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Concepts.Accounts;

public record CardEnabledOnAccount(bool Value) : ConceptAs<bool>(Value)
{
    public static implicit operator CardEnabledOnAccount(bool value) => new(value);
}
