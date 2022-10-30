// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Concepts.AccountHolders;
using Concepts.Accounts;

namespace Domain.Accounts.Debit;

public record AccountDetails(AccountName Name, AccountHolderId Owner, CardEnabledOnAccount IncludeCard);
