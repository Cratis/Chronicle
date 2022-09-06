// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Concepts.Accounts.Credit;

namespace Domain.Accounts.Debit;

[Route("/api/accounts/credit")]
public class CreditCards : Controller
{
    readonly IEventLog _eventLog;

    public CreditCards(IEventLog eventLog)
    {
        _eventLog = eventLog;
    }

    [HttpPost("apply")]
    public Task ApplyForCreditCard() => _eventLog.Branch(BranchTypes.CreditCardApplication);
}
