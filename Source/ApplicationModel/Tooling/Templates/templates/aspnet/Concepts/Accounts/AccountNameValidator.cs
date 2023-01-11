// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Concepts.Accounts;

public class AccountNameValidator : ConceptValidator<AccountName>
{
    public AccountNameValidator()
    {
        RuleFor(_ => _).Length(0, 16).WithMessage("Account name has to be less than 16 characters");
    }
}
