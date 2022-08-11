// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Concepts.Accounts.Credit;
using Events.Accounts.Credit.Application;

namespace Domain.Accounts.Debit;

[Route($"/api/accounts/credit/{CreditCardApplication.ApplicationIdValue}")]
public class CreditCardApplication : Controller
{
    public const string ApplicationIdValue = "applicationId";

    readonly IEventLog _eventLog;
    readonly IBranch _branch;

    public CreditCardApplication(IEventLog eventLog)
    {
        _eventLog = eventLog;
        var applicationId = (CreditCardApplicationId)RouteData.Values[ApplicationIdValue]!.ToString()!;
        var task = _eventLog.GetBranch(applicationId);
        task.Wait();
        _branch = task.Result;
    }

    [HttpPost("income/{income}")]
    public Task RegisterYearlyIncome(
        [FromRoute] CreditCardApplicationId applicationId,
        [FromRoute] double income) => _branch.Append(applicationId, new YearlyIncomeRegistered(income));

    [HttpPost("mortgage/{remaining}")]
    public Task AddMortgage(
        [FromRoute] CreditCardApplicationId applicationId,
        [FromRoute] double remaining) => _branch.Append(applicationId, new MortgageAdded(remaining));

    [HttpPost("carloan/{remaining}")]
    public Task AddCarLoan(
        [FromRoute] CreditCardApplicationId applicationId,
        [FromRoute] double remaining) => _branch.Append(applicationId, new CarLoanAdded(remaining));

    [HttpPost("consumerloan/{remaining}")]
    public Task AddConsumerLoan(
        [FromRoute] CreditCardApplicationId applicationId,
        [FromRoute] double remaining) => _branch.Append(applicationId, new ConsumerLoanAdded(remaining));
}
