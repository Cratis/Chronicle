// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Compliance;

namespace Sample.Customers
{
    public record SocialSecurityNumber(string Value) : PIIConceptAs<string>(Value)
    {
        public static implicit operator SocialSecurityNumber(string value) => new(value);
    }
}
