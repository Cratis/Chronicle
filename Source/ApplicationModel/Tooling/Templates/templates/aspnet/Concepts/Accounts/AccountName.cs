// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Concepts.Accounts
{
    public record AccountName(string Value) : ConceptAs<string>(Value)
    {
        public static implicit operator AccountName(string value) => new(value);
    }
}
