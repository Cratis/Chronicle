// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Concepts.Accounts.Credit;

public record CreditCardApplicationId(Guid Value) : ConceptAs<Guid>(Value)
{
    public static implicit operator CreditCardApplicationId(string value) => new(Guid.Parse(value));
    public static implicit operator CreditCardApplicationId(BranchId value) => new(value.Value);
    public static implicit operator BranchId(CreditCardApplicationId value) => new(value);
    public static implicit operator EventSourceId(CreditCardApplicationId value) => new(value.ToString());
}
