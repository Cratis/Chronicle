// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Events.Accounts.Credit;

namespace Domain.Accounts.Debit;

[Route("/api/accounts/credit")]
public class CreditAccounts : Controller
{
    readonly IEventLog _eventLog;

    public CreditAccounts(IEventLog eventLog) => _eventLog = eventLog;

    [HttpPost]
    public Task OpenCreditAccount([FromBody] OpenCreditAccount create) => _eventLog.Append(create.AccountId, new CreditAccountOpened(create.Details.Name, create.Details.Owner, create.Details.IncludeCard));
}
