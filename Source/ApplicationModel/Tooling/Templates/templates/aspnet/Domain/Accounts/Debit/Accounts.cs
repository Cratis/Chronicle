// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Concepts;
using Concepts.Accounts;
using Events.Accounts.Debit;

namespace Domain.Accounts.Debit
{
    [Route("/api/accounts/debit")]
    public class Accounts : Controller
    {
        readonly IEventLog _eventLog;

        public Accounts(IEventLog eventLog) => _eventLog = eventLog;

        [HttpPost]
        public Task Create([FromBody] CreateDebitAccount create) => _eventLog.Append(create.AccountId, new DebitAccountOpened(create.Name, create.Owner));

        [HttpPost("deposit")]
        public Task Deposit([FromBody] DepositToAccount deposit) => _eventLog.Append(deposit.AccountId, new DepositToDebitAccountPerformed(deposit.Amount));

        [HttpPost("withdraw")]
        public Task Withdraw([FromBody] WithdrawFromAccount deposit) => _eventLog.Append(deposit.AccountId, new WithdrawalFromDebitAccountPerformed(deposit.Amount));
    }
}
