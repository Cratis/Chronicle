// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Compliance;

namespace Dolittle.Customers
{
    public record LastName(string Value) : PIIConceptAs<string>(Value);
}
