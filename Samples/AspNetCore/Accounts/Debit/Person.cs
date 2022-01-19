// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Concepts;

namespace Sample.Accounts.Debit
{
    public record Person(Guid value) : ConceptAs<Guid>(value)
    {
        public static implicit operator Person(Guid value) => new (value);
    }
}
