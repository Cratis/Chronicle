// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Domain.Accounts.Debit;

[Route("/api/accounts/credit")]
public class CreditCardApplication : Controller
{
    [HttpPost("{income}")]
    public Task RegisterYearlyIncome([FromRoute] double income) => Task.CompletedTask;

    [HttpPost("{remaining}")]
    public Task AddMortgage([FromRoute] double remaining) => Task.CompletedTask;

    [HttpPost("{remaining}")]
    public Task AddCarLoan([FromRoute] double remaining) => Task.CompletedTask;

    [HttpPost("{remaining}")]
    public Task AddPersonalLoan([FromRoute] double remaining) => Task.CompletedTask;
}
