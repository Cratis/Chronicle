// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Concepts.AccountHolders;
using Concepts.Accounts;

namespace Read.Accounts.Debit;

public record DebitAccount(
    AccountId Id,
    AccountName Name,
    AccountHolderId AccountHolderId,
    AccountHolder AccountHolder,
    double? Balance,
    CardEnabledOnAccount HasCard,
    DateTimeOffset LastUpdated);
