// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Concepts.Accounts.Credit;
using Events.Accounts.Credit.Application;

namespace Domain.Accounts.Debit;

[Route($"/api/accounts/credit/application/{{{ApplicationIdValue}}}")]
public class CreditCardApplication : Controller
{
    const string ApplicationIdValue = "applicationId";

    readonly IEventLog _eventLog;

    public CreditCardApplication(IEventLog eventLog)
    {
        _eventLog = eventLog;
    }

    [HttpPost("income/{income}")]
    public Task RegisterYearlyIncome(
        [FromRoute] CreditCardApplicationId applicationId,
        [FromRoute] double income) => AppendToBranch(applicationId, new YearlyIncomeRegistered(income));

    [HttpPost("mortgage/{remaining}")]
    public Task AddMortgage(
        [FromRoute] CreditCardApplicationId applicationId,
        [FromRoute] double remaining) => AppendToBranch(applicationId, new MortgageAdded(remaining));

    [HttpPost("carloan/{remaining}")]
    public Task AddCarLoan(
        [FromRoute] CreditCardApplicationId applicationId,
        [FromRoute] double remaining) => AppendToBranch(applicationId, new CarLoanAdded(remaining));

    [HttpPost("consumerloan/{remaining}")]
    public Task AddConsumerLoan(
        [FromRoute] CreditCardApplicationId applicationId,
        [FromRoute] double remaining) => AppendToBranch(applicationId, new ConsumerLoanAdded(remaining));

    async Task AppendToBranch(CreditCardApplicationId applicationId, object @event)
    {
        var branch = await _eventLog.GetBranch(applicationId);
        await branch.Append(applicationId, @event);
    }
}
