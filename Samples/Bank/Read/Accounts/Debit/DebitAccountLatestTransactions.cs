// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Concepts.Accounts;

namespace Read.Accounts.Debit
{
    public record DebitAccountLatestTransactions(AccountId Id, IEnumerable<AccountTransaction> Transactions);
}
