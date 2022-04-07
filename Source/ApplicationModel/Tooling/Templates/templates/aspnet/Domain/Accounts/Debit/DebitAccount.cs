// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Concepts.Accounts;
using Events.Accounts.Debit;

namespace Domain.Accounts.Debit;

[Route("/api/accounts/debit/{accountId}")]
public class DebitAccount : Controller
{
    readonly IEventLog _eventLog;

    public DebitAccount(IEventLog eventLog) => _eventLog = eventLog;

    [HttpPost("name/{name}")]
    public Task SetDebitAccountName([FromRoute] AccountId accountId, [FromRoute] AccountName name) => _eventLog.Append(accountId, new DebitAccountNameChanged(name));

    [HttpPost("close")]
    public Task CloseDebitAccount([FromRoute] AccountId accountId) => _eventLog.Append(accountId, new DebitAccountClosed());

    [HttpPost("deposit/{amount}")]
    public Task DepositToAccount([FromRoute] AccountId accountId, [FromRoute] double amount) => _eventLog.Append(accountId, new DepositToDebitAccountPerformed(amount));

    [HttpPost("withdraw/{amount}")]
    public Task WithdrawFromAccount([FromRoute] AccountId accountId, [FromRoute] double amount) => _eventLog.Append(accountId, new WithdrawalFromDebitAccountPerformed(amount));
}
