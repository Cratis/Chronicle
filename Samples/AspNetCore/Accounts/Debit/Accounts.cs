// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events;
using Microsoft.AspNetCore.Mvc;

namespace Sample.Accounts.Debit
{
    [Route("/api/accounts/debit")]
    public class Accounts : Controller
    {
        readonly IEventStore _eventStore;

        public Accounts(IEventStore eventStore) => _eventStore = eventStore;

        [HttpPost]
        public Task Create([FromBody] OpenDebitAccount create) => _eventStore.DefaultEventLog.Append(create.AccountId, new DebitAccountOpened(create.Name, create.Owner));

        [HttpPost("deposit")]
        public Task Deposit([FromBody] DepositToAccount deposit) => _eventStore.DefaultEventLog.Append(deposit.AccountId, new DepositToDebitAccountPerformed(deposit.Amount));

        [HttpPost("withdraw")]
        public Task Withdraw([FromBody] WithdrawFromAccount deposit) => _eventStore.DefaultEventLog.Append(deposit.AccountId, new WithdrawalFromDebitAccountPerformed(deposit.Amount));
    }
}
