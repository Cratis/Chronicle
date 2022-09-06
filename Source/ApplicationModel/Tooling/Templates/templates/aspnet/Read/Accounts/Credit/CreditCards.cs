// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Concepts.Accounts.Credit;

namespace Read.Accounts.Debit;

[Route("/api/accounts/credit")]
public class CreditCards : Controller
{
    readonly IEventLog _eventLog;

    public CreditCards(IEventLog eventLog)
    {
        _eventLog = eventLog;
    }

    [HttpGet("applications")]
    public async Task<IEnumerable<CreditCardApplication>> Applications()
    {
        var branches = await _eventLog.GetBranchesFor(BranchTypes.CreditCardApplication);
        return branches.Select(_ => new CreditCardApplication(_.Identifier, _.Started));
    }
}
