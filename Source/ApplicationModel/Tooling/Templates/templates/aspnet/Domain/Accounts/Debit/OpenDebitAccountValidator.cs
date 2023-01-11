// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Domain.Accounts.Debit;

public class OpenDebitAccountValidator : CommandValidator<OpenDebitAccount>
{
    public OpenDebitAccountValidator()
    {
        RuleFor(_ => _.Details.Name).NotEmpty().WithMessage("Name is required");
        RuleFor(_ => _.Details.Owner).NotNull().WithMessage("Owner is required");
        RuleFor(_ => _.Details.IncludeCard).NotNull().WithMessage("Include card should be specified");
    }
}
